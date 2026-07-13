using MediatR;
using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Application.Queries;

namespace ScriptFlow.API.Application.Handlers;

public sealed class GetPrescriptionDailyVolumeQueryHandler
    : IRequestHandler<GetPrescriptionDailyVolumeQuery, IReadOnlyCollection<PrescriptionDailyVolumeDto>>
{
    private const int WindowDays = 14;

    private readonly IPrescriptionRepository _prescriptions;

    public GetPrescriptionDailyVolumeQueryHandler(IPrescriptionRepository prescriptions)
    {
        _prescriptions = prescriptions;
    }

    public async Task<IReadOnlyCollection<PrescriptionDailyVolumeDto>> Handle(
        GetPrescriptionDailyVolumeQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var since = today.AddDays(-(WindowDays - 1));

        var rows = await _prescriptions.GetDailyVolumeAsync(since.ToDateTime(TimeOnly.MinValue), cancellationToken);
        var countsByDate = rows.ToDictionary(r => r.Date, r => r.Count);

        // Every day in the window gets an entry even at zero, so the frontend's trend chart
        // doesn't have to guess which days are missing - same approach as GetStatusCountsAsync.
        return Enumerable.Range(0, WindowDays)
            .Select(offset => since.AddDays(offset))
            .Select(date => new PrescriptionDailyVolumeDto(date, countsByDate.GetValueOrDefault(date)))
            .ToList();
    }
}
