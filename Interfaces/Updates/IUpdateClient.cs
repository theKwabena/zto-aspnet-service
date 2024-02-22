namespace MigrateClient.Interfaces.Updates
{
    public interface IUpdateClient
    {
        Task SendMessage(string message);
    }
}
