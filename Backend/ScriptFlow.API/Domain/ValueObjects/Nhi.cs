using System.Text.RegularExpressions;
using ScriptFlow.API.Domain.Exceptions;

namespace ScriptFlow.API.Domain.ValueObjects;

/// <summary>New Zealand National Health Index number: 3 letters followed by 4 digits.</summary>
public sealed record Nhi
{
    private static readonly Regex Pattern = new("^[A-Z]{3}[0-9]{4}$", RegexOptions.Compiled);

    public string Value { get; }

    public Nhi(string value)
    {
        var normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!Pattern.IsMatch(normalized))
        {
            throw new DomainException(
                $"'{value}' is not a valid NHI. Expected format: 3 letters followed by 4 digits (e.g. ABC1234).");
        }

        Value = normalized;
    }

    public override string ToString() => Value;
}
