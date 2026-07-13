namespace ScriptFlow.API.Application.DTOs;

public sealed record PrescriptionDailyVolumeDto(DateOnly Date, int Count);
