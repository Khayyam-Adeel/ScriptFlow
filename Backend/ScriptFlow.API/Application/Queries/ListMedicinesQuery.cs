using MediatR;
using ScriptFlow.API.Application.DTOs;

namespace ScriptFlow.API.Application.Queries;

/// <summary>Lists medicines for medication-line pickers, optionally filtered by a name/SCTID search term.</summary>
public sealed record ListMedicinesQuery(string? Search) : IRequest<IReadOnlyCollection<MedicineDto>>;
