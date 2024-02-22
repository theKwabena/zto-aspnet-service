using System.Text.Json;
using MigrateClient.Data.Models;
using MigrateClient.Interfaces;
using MigrateClient.Interfaces.Background;
using MigrateClient.Interfaces.Exchange;
using MigrateClient.Services.Exchange;
using MigrateClient.Services.Job;
using StackExchange.Redis;

namespace MigrateClient.Services.Background
{
    public class BackgroundTaskService : BackgroundService
    {
        
        private readonly IExchangeWrapper _ews;
        private readonly IQueueService _queue;

        public BackgroundTaskService(IExchangeWrapper ews, IQueueService queue)
        {
            _ews = ews;
            _queue = queue;

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {

                var job = await _queue.DequeueJobAsync();
                if (job != null)
                {
                    await _ews.ImportEmails(job.Username);
                    await _queue.JobCompletedAsync(job);
                };
              
                // Implementation using C# channels
                // var workItem = await _taskQueue.DequeueAsync(stoppingToken);
                //
                // try
                // {
                //     await workItem(stoppingToken);
                //     
                // }
                // catch (Exception ex)
                // {
                //     // Log the exception if necessary
                // }
            }
        }
        
        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            await base.StopAsync(stoppingToken);
        }
        
       
    }

}
