using Crm.Application.DTOs;
using Crm.Application.Exceptions;
using Crm.Application.Services;
using Crm.Domain.Entities;
using Crm.Domain.Enums;
using Crm.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Crm.Tests;

public sealed class AuthServiceTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly CrmDbContext _db;
    private readonly IdentityPasswordHasher _passwordHasher = new();
    private readonly AuthService _service;
    private readonly User _admin;
    private readonly Agent _agent;

    public AuthServiceTests()
    {
        _connection.Open();
        _db = new CrmDbContext(new DbContextOptionsBuilder<CrmDbContext>()
            .UseSqlite(_connection)
            .Options);
        _db.Database.EnsureCreated();
        _service = new AuthService(_db, _passwordHasher);

        _admin = new User
        {
            Email = "admin@crm.local",
            DisplayName = "Dev Admin",
            PasswordHash = _passwordHasher.Hash("Admin123!"),
            Role = UserRole.Admin,
            IsActive = true
        };
        _agent = new Agent { Name = "Sales Assistant", IsActive = true };

        _db.AddRange(_admin, _agent);
        _db.SaveChanges();
    }

    [Fact]
    public void PasswordHasher_verifies_correct_password_and_rejects_wrong_one()
    {
        var hash = _passwordHasher.Hash("S3cret!pass");

        hash.Should().NotContain("S3cret!pass");
        _passwordHasher.Verify(hash, "S3cret!pass").Should().BeTrue();
        _passwordHasher.Verify(hash, "wrong-password").Should().BeFalse();
    }

    [Fact]
    public async Task ValidateCredentials_returns_user_for_valid_login()
    {
        var user = await _service.ValidateCredentialsAsync(new LoginRequest
        {
            Email = "Admin@CRM.local",
            Password = "Admin123!"
        });

        user.Id.Should().Be(_admin.Id);
        user.Email.Should().Be("admin@crm.local");
        user.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public async Task ValidateCredentials_rejects_wrong_password()
    {
        var act = () => _service.ValidateCredentialsAsync(new LoginRequest
        {
            Email = "admin@crm.local",
            Password = "not-the-password"
        });

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task ValidateCredentials_rejects_unknown_email()
    {
        var act = () => _service.ValidateCredentialsAsync(new LoginRequest
        {
            Email = "ghost@crm.local",
            Password = "Admin123!"
        });

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task ValidateCredentials_rejects_inactive_user()
    {
        _admin.IsActive = false;
        await _db.SaveChangesAsync();

        var act = () => _service.ValidateCredentialsAsync(new LoginRequest
        {
            Email = "admin@crm.local",
            Password = "Admin123!"
        });

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task IssueAgentApiKey_returns_plaintext_once_and_stores_only_hash()
    {
        var issued = await _service.IssueAgentApiKeyAsync(_agent.Id);

        issued.AgentId.Should().Be(_agent.Id);
        issued.ApiKey.Should().StartWith("crm_");

        var stored = _db.Agents.Single(x => x.Id == _agent.Id).ApiKeyHash;
        stored.Should().NotBeNullOrEmpty();
        stored.Should().NotBe(issued.ApiKey);
        stored.Should().Be(AuthService.ComputeApiKeyHash(issued.ApiKey));
    }

    [Fact]
    public async Task ResolveAgentByApiKey_returns_agent_for_valid_key_and_null_for_invalid()
    {
        var issued = await _service.IssueAgentApiKeyAsync(_agent.Id);

        var resolved = await _service.ResolveAgentByApiKeyAsync(issued.ApiKey);
        var unknown = await _service.ResolveAgentByApiKeyAsync("crm_definitely-not-a-key");

        resolved.Should().NotBeNull();
        resolved!.Id.Should().Be(_agent.Id);
        unknown.Should().BeNull();
    }

    [Fact]
    public async Task RotatingApiKey_invalidates_previous_key()
    {
        var first = await _service.IssueAgentApiKeyAsync(_agent.Id);
        var second = await _service.IssueAgentApiKeyAsync(_agent.Id);

        (await _service.ResolveAgentByApiKeyAsync(first.ApiKey)).Should().BeNull();
        (await _service.ResolveAgentByApiKeyAsync(second.ApiKey)).Should().NotBeNull();
    }

    [Fact]
    public async Task ResolveAgentByApiKey_ignores_inactive_agents()
    {
        var issued = await _service.IssueAgentApiKeyAsync(_agent.Id);
        _db.Agents.Single(x => x.Id == _agent.Id).IsActive = false;
        await _db.SaveChangesAsync();

        (await _service.ResolveAgentByApiKeyAsync(issued.ApiKey)).Should().BeNull();
    }

    [Fact]
    public async Task IssueAgentApiKey_for_unknown_agent_throws_not_found()
    {
        var act = () => _service.IssueAgentApiKeyAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
