using Shared.contract.Enums;

namespace ScriptFlow.API.Application.DTOs;

public sealed record PrescriptionStatusCountDto(PrescriptionStatus Status, int Count);
