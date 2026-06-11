using Crm.Application.Interfaces;
using Crm.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Crm.Application.Services;

/// <summary>
/// PBKDF2 password hashing via ASP.NET Core Identity's <see cref="PasswordHasher{TUser}"/>.
/// </summary>
public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private static readonly PasswordHasher<User> Hasher = new();
    private static readonly User Placeholder = new();

    public string Hash(string password) => Hasher.HashPassword(Placeholder, password);

    public bool Verify(string hash, string password) =>
        Hasher.VerifyHashedPassword(Placeholder, hash, password) is not PasswordVerificationResult.Failed;
}
