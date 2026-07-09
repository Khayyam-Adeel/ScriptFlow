using MediatR;
using ScriptFlow.API.Application.Commands;
using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Domain.Entities;
using ScriptFlow.API.Domain.Exceptions;

namespace ScriptFlow.API.Application.Handlers;

public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, AuthResponse>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RegisterUserCommandHandler(IUserRepository users, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var existing = await _users.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
        {
            throw new DomainException($"A user with email '{request.Email}' already exists.");
        }

        var user = new User(Guid.NewGuid(), request.Email, _passwordHasher.Hash(request.Password));
        await _users.AddAsync(user, cancellationToken);

        var (token, expiresAtUtc) = _jwtTokenGenerator.Generate(user);
        return new AuthResponse(user.Email, token, expiresAtUtc);
    }
}
