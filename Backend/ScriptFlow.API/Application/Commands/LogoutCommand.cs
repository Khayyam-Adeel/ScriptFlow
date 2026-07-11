using MediatR;

namespace ScriptFlow.API.Application.Commands;

public sealed record LogoutCommand : IRequest;
