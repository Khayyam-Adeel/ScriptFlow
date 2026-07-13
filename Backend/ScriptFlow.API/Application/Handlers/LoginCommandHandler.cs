using MediatR;
using ScriptFlow.API.Application.Commands;
using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Application.Interfaces;

namespace ScriptFlow.API.Application.Handlers;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(IUserRepository users, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || !_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var (token, expiresAtUtc) = _jwtTokenGenerator.Generate(user);
        return new AuthResponse(user.Email, user.Role.ToString(), token, expiresAtUtc);
    }
}
