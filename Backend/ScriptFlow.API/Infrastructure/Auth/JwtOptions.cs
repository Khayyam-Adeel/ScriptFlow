namespace ScriptFlow.API.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "ScriptFlow.API";
    public string Audience { get; set; } = "ScriptFlow.Clients";
    public string SigningKey { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
}
