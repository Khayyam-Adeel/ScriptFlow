using Microsoft.AspNetCore.Identity;
using ScriptFlow.API.Application.Interfaces;
using IdentityPasswordHasher = Microsoft.AspNetCore.Identity.PasswordHasher<ScriptFlow.API.Domain.Entities.User>;

namespace ScriptFlow.API.Infrastructure.Auth;

/// <summary>Wraps ASP.NET Core Identity's PBKDF2 hasher without pulling in full Identity/EF.</summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private readonly IdentityPasswordHasher _identityHasher = new();

    public string Hash(string password) => _identityHasher.HashPassword(default!, password);

    public bool Verify(string passwordHash, string providedPassword)
    {
        var result = _identityHasher.VerifyHashedPassword(default!, passwordHash, providedPassword);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
