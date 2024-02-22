using Microsoft.Exchange.WebServices.Data;
using Microsoft.Identity.Client;
using MigrateClient.Interfaces;
using MigrateClient.Interfaces.Background;
using MigrateClient.Interfaces.Exchange;
using MigrateClient.Services.Background;
using MigrateClient.Services.Exchange;
using MigrateClient.Services.Job;
using StackExchange.Redis;

namespace MigrateClient.Extensions
{
    public static class ServiceExtension
    {
        public static void ConfigureEwsClient(this IServiceCollection services) =>
               services.AddSingleton<IExchangeWrapper, ExchangeWrapper>();

        public static void ConfigureBackgroundService(this IServiceCollection services)
        {
            // services.AddSingleton<IBackgroundTaskQueue>(ctx =>
            // {
            //     return new BackgroundTaskQueue(100);
            // });

            services.AddSingleton<IQueueService, QueueService>();
            services.AddHostedService<BackgroundTaskService>();
        }

        public static void ConfigureRedisService(this IServiceCollection services)
        {
            var connectionOptions = Environment.GetEnvironmentVariable("REDIS_DATABASE_URL");
            Console.WriteLine(connectionOptions);
            services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(connectionOptions));
        }
           

    }
} 
    