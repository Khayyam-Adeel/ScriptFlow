using MediatR;
using ScriptFlow.API.Application.DTOs;

namespace ScriptFlow.API.Application.Queries;

/// <summary>Lists all practices, for practice/practice-location pickers.</summary>
public sealed record ListPracticesQuery : IRequest<IReadOnlyCollection<PracticeDto>>;
