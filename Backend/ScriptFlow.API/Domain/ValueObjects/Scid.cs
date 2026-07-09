using System.Security.Cryptography;
using System.Text.RegularExpressions;
using ScriptFlow.API.Domain.Exceptions;

namespace ScriptFlow.API.Domain.ValueObjects;

/// <summary>
/// The prescription's own unique identifier (SCID) — not to be confused with a medicine's
/// SNOMED CT code (SCTID). Format: "9" + 5-char EPS entity no + 5 alphanumeric chars.
/// </summary>
public sealed record Scid
{
    private const string AllowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private static readonly Regex Pattern = new("^9[A-Za-z0-9]{5}[A-Za-z0-9]{5}$", RegexOptions.Compiled);

    public string Value { get; }

    public Scid(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Pattern.IsMatch(value))
        {
            throw new DomainException(
                $"'{value}' is not a valid SCID. Expected format: 9 followed by a 5-character " +
                "EPS entity number and 5 alphanumeric characters (e.g. 9J0BGVA1B2C).");
        }

        Value = value.ToUpperInvariant();
    }

    public static Scid Generate() => new($"9{RandomAlphaNumeric(10)}");

    private static string RandomAlphaNumeric(int length)
    {
        Span<char> chars = stackalloc char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = AllowedChars[RandomNumberGenerator.GetInt32(AllowedChars.Length)];
        }

        return new string(chars);
    }

    public override string ToString() => Value;
}
