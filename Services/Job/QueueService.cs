using System.Diagnostics;
using System.Text.Json;
using MigrateClient.Data.Models;
using MigrateClient.Interfaces;
using StackExchange.Redis;

namespace MigrateClient.Services.Job;

public class QueueService : IQueueService
{
    private readonly IDatabase _redisDatabase;
    private readonly ILogger<QueueService> _logger;

    public QueueService(IConnectionMultiplexer multiplexer, ILogger<QueueService> logger)
    {
        _redisDatabase = multiplexer.GetDatabase();
        _logger = logger;
    }


    public async Task EnqueueJobAsync(JobModel job)
    {
        await _redisDatabase.ListLeftPushAsync("jobQueue", JsonSerializer.Serialize(job));
        await _redisDatabase.ListLeftPushAsync("jobs", JsonSerializer.Serialize(job));
        await _redisDatabase.HashSetAsync(job.Username, "status", "queued");
    }

    public async Task<List<JobModel>> GetJobsInQueueAsync()
    {
        var jobs = await _redisDatabase.ListRangeAsync("jobQueue");
        var jobList = new List<JobModel>();

        
        foreach (var job in jobs)
        {
            var redisJob = JsonSerializer.Deserialize<JobModel>(job!);
            redisJob!.Status = _redisDatabase.HashGet(redisJob.Username, "status")!;
            jobList.Add(redisJob);
        }
       
        

        return jobList;
    }
    
    public async Task<List<JobModel>> GetJobsAsync()
    {
        var jobs = await _redisDatabase.ListRangeAsync("jobs");
        var jobList = new List<JobModel>();

        
        foreach (var job in jobs)
        {
            var redisJob = JsonSerializer.Deserialize<JobModel>(job!);
            redisJob!.Status = _redisDatabase.HashGet(redisJob.Username, "status")!;
            jobList.Add(redisJob);
        }

        return jobList;
    }
    
    public async Task<JobModel?> DequeueJobAsync()
    {
        var job = await _redisDatabase.ListGetByIndexAsync("jobQueue", 0);
        if (!job.HasValue)
        {
            return null;
        }
        var redisJob = JsonSerializer.Deserialize<JobModel?>(job!);
        return redisJob;
    }
   

    public async Task UpdateJobStatusAsync(string username, string status, string message="")
    {
        HashEntry[] progress = new HashEntry[]{
            new HashEntry("status", status),
            new HashEntry("message", message)
        };
        await _redisDatabase.HashSetAsync(username, progress);
        
    }
    
    public async Task JobCompletedAsync(JobModel job)
    {
        await _redisDatabase.ListRemoveAsync("jobQueue", JsonSerializer.Serialize(job));
        await _redisDatabase.KeyDeleteAsync(job.JobId);
        await UpdateJobStatusAsync(job.Username, "Completed");
    }
    
}