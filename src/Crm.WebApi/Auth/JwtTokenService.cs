using System.Text;
using Crm.Application.DTOs;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Crm.WebApi.Auth;

public sealed class JwtTokenService(JwtOptions options)
{
    public LoginResponseDto CreateToken(UserDto user)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(options.ExpiryMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
                SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                [JwtRegisteredClaimNames.Email] = user.Email,
                [JwtRegisteredClaimNames.Name] = user.DisplayName,
                [AuthConstants.RoleClaim] = user.Role.ToString(),
                [AuthConstants.ActorTypeClaim] = AuthConstants.UserActorType
            }
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new LoginResponseDto(token, expiresAt, user);
    }
}
