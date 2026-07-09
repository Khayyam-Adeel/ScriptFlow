namespace Shared.Infrastructure.Correlation;

public interface ICorrelationIdAccessor
{
    string CorrelationId { get; }
}
