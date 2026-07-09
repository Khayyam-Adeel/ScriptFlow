using System.Collections.Concurrent;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Domain.Entities;

namespace ScriptFlow.API.Infrastructure.Persistence;

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<string, User> _byEmail = new(StringComparer.OrdinalIgnoreCase);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => Task.FromResult(_byEmail.GetValueOrDefault(email.Trim()));

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _byEmail[user.Email] = user;
        return Task.CompletedTask;
    }
}
