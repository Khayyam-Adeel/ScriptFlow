using MediatR;
using ScriptFlow.API.Application.DTOs;

namespace ScriptFlow.API.Application.Commands;

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;
