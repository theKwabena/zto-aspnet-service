using Microsoft.AspNetCore.SignalR;
using MigrateClient.Interfaces.Updates;

namespace MigrateClient.Services.Updates
{
    public class Updates : Hub<IUpdateClient>
    {
        public async Task SendMessage(string message)
        {
            await Clients.Caller.SendMessage(message);
        }
    }
}
