using Crm.Domain.Entities;

namespace Crm.Application.Services;

/// <summary>
/// Shared conversation grouping rules used by the conversation projection
/// in <see cref="CrmService"/> and by heartbeat trigger detection.
/// </summary>
internal static class ConversationHelper
{
    public static string GetConversationId(Message message)
    {
        if (message.ContactId is not null)
        {
            return $"contact:{message.ContactId.Value:N}";
        }

        if (message.DealId is not null)
        {
            return $"deal:{message.DealId.Value:N}";
        }

        if (!string.IsNullOrWhiteSpace(message.ExternalMessageId))
        {
            return $"{message.Channel}:{message.ExternalMessageId.Trim()}";
        }

        return $"message:{message.Id:N}";
    }

    public static DateTimeOffset GetTimestamp(Message message) =>
        message.ReceivedAt ?? message.SentAt ?? message.CreatedAt;
}
