using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ScriptFlow.API.Application.Interfaces;

namespace ScriptFlow.API.Infrastructure.Auth;

public sealed class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User
                ?? throw new InvalidOperationException("No authenticated user is available on the current request.");

            // JwtTokenGenerator issues the "sub" claim; depending on JWT bearer claim-mapping
            // configuration it may surface here as either name, so check both.
            var claim = user.FindFirst(JwtRegisteredClaimNames.Sub) ?? user.FindFirst(ClaimTypes.NameIdentifier);
            if (claim is null || !Guid.TryParse(claim.Value, out var userId))
            {
                throw new InvalidOperationException("The authenticated user's id claim is missing or invalid.");
            }

            return userId;
        }
    }

    public Guid? UserIdOrNull
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var claim = user?.FindFirst(JwtRegisteredClaimNames.Sub) ?? user?.FindFirst(ClaimTypes.NameIdentifier);
            return claim is not null && Guid.TryParse(claim.Value, out var userId) ? userId : null;
        }
    }
}
