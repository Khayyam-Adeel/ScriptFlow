using System.Text.RegularExpressions;
using ScriptFlow.API.Domain.Exceptions;

namespace ScriptFlow.API.Domain.ValueObjects;

/// <summary>
/// Health Point Identifier for a practice location, made of two parts: the HPI No
/// (3 letters + 2 digits, e.g. "FZZ99") and a single-letter HPI Extension (e.g. "B"),
/// displayed/stored together as "FZZ99-B".
/// </summary>
public sealed record HpiNumber
{
    private static readonly Regex HpiNoPattern = new("^[A-Z]{3}[0-9]{2}$", RegexOptions.Compiled);
    private static readonly Regex ExtensionPattern = new("^[A-Z]$", RegexOptions.Compiled);

    public string HpiNo { get; }
    public string HpiExtension { get; }
    public string Value => $"{HpiNo}-{HpiExtension}";

    public HpiNumber(string hpiNo, string hpiExtension)
    {
        var normalizedNo = hpiNo?.Trim().ToUpperInvariant() ?? string.Empty;
        var normalizedExtension = hpiExtension?.Trim().ToUpperInvariant() ?? string.Empty;

        if (!HpiNoPattern.IsMatch(normalizedNo))
        {
            throw new DomainException(
                $"'{hpiNo}' is not a valid HPI No. Expected format: 3 letters followed by 2 digits (e.g. FZZ99).");
        }

        if (!ExtensionPattern.IsMatch(normalizedExtension))
        {
            throw new DomainException(
                $"'{hpiExtension}' is not a valid HPI Extension. Expected a single letter (e.g. B).");
        }

        HpiNo = normalizedNo;
        HpiExtension = normalizedExtension;
    }

    public static HpiNumber Parse(string combined)
    {
        var parts = combined?.Split('-') ?? Array.Empty<string>();
        if (parts.Length != 2)
        {
            throw new DomainException($"'{combined}' is not a valid HPI number. Expected format: FZZ99-B.");
        }

        return new HpiNumber(parts[0], parts[1]);
    }

    public override string ToString() => Value;
}
