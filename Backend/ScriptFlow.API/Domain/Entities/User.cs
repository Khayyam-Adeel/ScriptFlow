using ScriptFlow.API.Domain.Exceptions;

namespace ScriptFlow.API.Domain.Entities;

/// <summary>Login identity used to authenticate against the API (separate from a Provider profile).</summary>
public sealed class User
{
    public Guid Id { get; }
    public string Email { get; }
    public string PasswordHash { get; }

    public User(Guid id, string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash is required.");
        }

        Id = id;
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
    }
}
