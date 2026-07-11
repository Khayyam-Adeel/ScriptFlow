namespace Shared.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;

    // No default: forces every environment to supply real credentials via config/env vars
    // rather than silently connecting with the broker's out-of-the-box guest/guest account.
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ExchangeName { get; set; } = "scriptflow.events";
}
