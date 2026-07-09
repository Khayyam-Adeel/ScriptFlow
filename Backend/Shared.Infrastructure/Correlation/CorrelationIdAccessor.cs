namespace Shared.Infrastructure.Correlation;

public sealed class CorrelationIdAccessor : ICorrelationIdAccessor
{
    private static readonly AsyncLocal<string?> Current = new();

    public string CorrelationId => Current.Value ?? string.Empty;

    internal static void Set(string correlationId) => Current.Value = correlationId;
}
