using MediatR;
using ScriptFlow.API.Application.Commands;
using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Domain.Entities;
using ScriptFlow.API.Domain.Exceptions;
using Shared.contract.Enums;

namespace ScriptFlow.API.Application.Handlers;

public sealed class RegisterAdminUserCommandHandler : IRequestHandler<RegisterAdminUserCommand, CreatedUserDto>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterAdminUserCommandHandler(IUserRepository users, IPasswordHasher passwordHasher)
    {
        _users = users;
        _passwordHasher = passwordHasher;
    }

    public async Task<CreatedUserDto> Handle(RegisterAdminUserCommand request, CancellationToken cancellationToken)
    {
        var existing = await _users.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
        {
            throw new DomainException($"A user with email '{request.Email}' already exists.");
        }

        var user = new User(Guid.NewGuid(), request.Email, _passwordHasher.Hash(request.Password), UserRole.Admin);
        await _users.AddAsync(user, cancellationToken);

        return new CreatedUserDto(user.Id, user.Email, user.Role.ToString());
    }
}
