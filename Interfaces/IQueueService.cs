using MigrateClient.Data.Models;

namespace MigrateClient.Interfaces;

public interface IQueueService
{
    Task EnqueueJobAsync(JobModel job);
    Task<List<JobModel>> GetJobsAsync();
    Task<List<JobModel>> GetJobsInQueueAsync();
    Task<JobModel?> DequeueJobAsync();
    Task UpdateJobStatusAsync(string username, string status, string message = "");
    Task JobCompletedAsync(JobModel job);
}