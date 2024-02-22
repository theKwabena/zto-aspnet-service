namespace MigrateClient.Interfaces.Exchange
{
    public interface IExchangeWrapper
    {
        Task Initialization { get; }
        Task ImportEmails(string username); // initially username and path

    }
}
