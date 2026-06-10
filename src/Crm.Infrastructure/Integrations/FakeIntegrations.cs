using Crm.Application.Interfaces;

namespace Crm.Infrastructure.Integrations;

public sealed class FakeMessageSender : IMessageSender
{
    public Task SendAsync(string channel, string recipient, string text, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

public sealed class FakeExternalCrmSyncService : IExternalCrmSyncService
{
    public Task SyncAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public sealed class FakeAgentRuntime : IAgentRuntime
{
    public Task<string> SummarizeAsync(string input, CancellationToken cancellationToken = default) =>
        Task.FromResult(input.Length <= 240 ? input : input[..240]);
}

public sealed class FakeEmbeddingService : IEmbeddingService
{
    public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default) =>
        Task.FromResult(Array.Empty<float>());
}
