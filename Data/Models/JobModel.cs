namespace MigrateClient.Data.Models;

public class JobModel
{
    public string? JobId { get; set; } = Guid.NewGuid().ToString();
    public string Username { get; set; } = string.Empty;
    public string? Status { get; set; } = string.Empty;
}