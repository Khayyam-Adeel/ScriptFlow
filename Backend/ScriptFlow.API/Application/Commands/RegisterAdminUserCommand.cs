using MediatR;
using ScriptFlow.API.Application.DTOs;

namespace ScriptFlow.API.Application.Commands;

/// <summary>Admin-only user creation (see AuthController.RegisterAdmin) - the counterpart to
/// RegisterUserCommand, which always creates a Prescriber. Never returns a token: the caller
/// stays signed in as themselves, they're not signing in as the account they just created.</summary>
public sealed record RegisterAdminUserCommand(string Email, string Password) : IRequest<CreatedUserDto>;
