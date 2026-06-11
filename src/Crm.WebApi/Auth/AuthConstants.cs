namespace Crm.WebApi.Auth;

public static class AuthConstants
{
    /// <summary>Authentication scheme for AI agents using the X-Api-Key header.</summary>
    public const string AgentApiKeyScheme = "AgentApiKey";

    public const string ApiKeyHeader = "X-Api-Key";

    /// <summary>Claim distinguishing human users from agents: "user" or "agent".</summary>
    public const string ActorTypeClaim = "crm:actor_type";

    public const string AgentIdClaim = "crm:agent_id";
    public const string RoleClaim = "role";
    public const string UserActorType = "user";
    public const string AgentActorType = "agent";

    public static class Policies
    {
        /// <summary>Any authenticated human (JWT). Applied to all mutating endpoints by convention.</summary>
        public const string HumanOnly = "HumanOnly";

        /// <summary>Authenticated human with the Admin role.</summary>
        public const string AdminOnly = "AdminOnly";

        /// <summary>Authenticated human or agent. Used for reading and for proposing agent actions.</summary>
        public const string HumanOrAgent = "HumanOrAgent";
    }
}
