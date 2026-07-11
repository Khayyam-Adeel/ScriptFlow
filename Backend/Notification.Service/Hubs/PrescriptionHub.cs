using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Notification.Service.Hubs;

// Server-push only: PrescriptionStatusChangedEventHandler broadcasts through this hub's
// IHubContext, clients never invoke methods on it. [Authorize] keeps the socket restricted
// to the same JWT-bearing users who can already call ScriptFlow.API.
[Authorize]
public sealed class PrescriptionHub : Hub
{
}
