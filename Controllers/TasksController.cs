using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using MigrateClient.Data.DTOs;
using MigrateClient.Data.Models;
using MigrateClient.Extensions;
using MigrateClient.Interfaces;
using StackExchange.Redis;

namespace MigrateClient.Controllers;

[ApiVersion(1)]
[Route("api/v{v:apiVersion}/tasks")]
[ApiController]
public class TasksController : ControllerBase
{
    
    private readonly IQueueService _redisQueue;
    private readonly ILogger<TasksController> _logger;
    private const int MessageStreamRetryTimeout = 5000; // milliseconds
    private const int MessageStreamDelay = 3000; //

    public TasksController(IQueueService redisQueue, ILogger<TasksController> logger)
    {
        _redisQueue = redisQueue;
        _logger = logger;
    }
    
    
    [HttpGet()]
    public async Task<List<JobModel>> Jobs()
    {
        return await _redisQueue.GetJobsAsync();
    }
        
    [HttpGet("in-queue")]
    public async Task<List<JobModel>> JobsInQueue()
    {
        return await _redisQueue.GetJobsInQueueAsync();
    }
    
    [HttpGet("sse")]
    public async Task SendSse(string userId, CancellationToken ctx)
    {
        await HttpContext.SseInitAsync();
       
        while (!ctx.IsCancellationRequested)
        {
            var job = await _redisQueue.GetJobStatus(userId);
            if (!job.HasValue)
            {
                await HttpContext.SendEventAsync( new SseModel("end_stream", $"Job with id {userId}" + $"not found")
                {
                    Id = userId,
                    Retry = MessageStreamRetryTimeout
                });
                break;
            } else if (job.ToString() == "SUCCESS" || job.ToString() == "FAILURE")
            {
                await SendSseEvent("end_stream", job.ToString()!, userId);
                break;
            }
            else
            {
                await SendSseEvent("new_data", job.ToString()!, userId);
                break;
            }
            
            await Task.Delay(MessageStreamDelay, ctx);
        }
        
            
    }
    
    
    private async Task SendSseEvent(string eventType, string eventData, string userId)
    {
        await HttpContext.SendEventAsync(new SseModel(eventType, eventData)
        {
            Id = userId,
            Retry = MessageStreamRetryTimeout
        });
    }
    
}