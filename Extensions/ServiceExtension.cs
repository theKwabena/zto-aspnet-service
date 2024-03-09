using System.Text.Json;
using Asp.Versioning;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.Identity.Client;
using MigrateClient.Data.Models;
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
        
        public static void ConfigureVersioning(this IServiceCollection services)
        {
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1);
                options.ReportApiVersions = true;
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new UrlSegmentApiVersionReader(),
                    new HeaderApiVersionReader("X-Api-Version"));
            }).AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'V";
                options.SubstituteApiVersionInUrl = true;

            });
        }

        public static void ConfigureCors(this IServiceCollection services) =>
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                    builder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                );
            });
        
        
        public static async System.Threading.Tasks.Task SseInitAsync(this HttpContext ctx)
        {
            ctx.Response.Headers.Append("Cache-Control", "no-cache");
            ctx.Response.ContentType=  "text/event-stream";
            await ctx.Response.Body.FlushAsync();
        }
        
        public static async System.Threading.Tasks.Task SendEventAsync(this HttpContext ctx, SseModel e)
        {
            await ctx.Response.WriteAsync("event: " + e.Name + "\n");

            var lines = e.Data switch
            {
                null        => new [] { String.Empty },
                string s    => s.Split('\n').ToArray(),
                _           => new [] { JsonSerializer.Serialize(e.Data) }
            };

            foreach(var line in lines)
                await ctx.Response.WriteAsync("data: " + line + "\n");

            await ctx.Response.WriteAsync("\n");
            await ctx.Response.Body.FlushAsync();
        }
           

    }
} 
    