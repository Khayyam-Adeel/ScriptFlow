using MediatR;
using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Application.Mappings;
using ScriptFlow.API.Application.Queries;

namespace ScriptFlow.API.Application.Handlers;

public sealed class ListMedicinesQueryHandler : IRequestHandler<ListMedicinesQuery, IReadOnlyCollection<MedicineDto>>
{
    private readonly IMedicineRepository _medicines;

    public ListMedicinesQueryHandler(IMedicineRepository medicines)
    {
        _medicines = medicines;
    }

    public async Task<IReadOnlyCollection<MedicineDto>> Handle(ListMedicinesQuery request, CancellationToken cancellationToken)
    {
        var medicines = await _medicines.ListAsync(request.Search, cancellationToken);
        return medicines.Select(m => m.ToDto()).ToList();
    }
}
