using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using MigrateClient.Data.DTOs;
using MigrateClient.Data.Models;
using MigrateClient.Extensions;
using MigrateClient.Interfaces;
using MigrateClient.Interfaces.Exchange;
using StackExchange.Redis;


namespace MigrateClient.Controllers
{
    [ApiVersion(1)]
    [Route("api/v{v:apiVersion}/migrate")]
    [ApiController]
    public class ImportController : ControllerBase
    {
        private readonly IExchangeWrapper _ewsClient;
        public static readonly string _folderPath = "/home/mailboxes/mailbox-extracts/";
        private readonly IQueueService _redisQueue;
        private readonly IDatabase _redisDatabase;
        private readonly ILogger<ImportController> _logger;
        public ImportController(
            IExchangeWrapper ewsClient, IQueueService queue, IConnectionMultiplexer multiplexer,
            ILogger<ImportController> logger) //IBackgroundTaskQueue taskQueue)
        {
            _ewsClient = ewsClient;
            // _backgroundTaskQueue = taskQueue;
            _redisQueue = queue;
            _redisDatabase = multiplexer.GetDatabase();
            _logger = logger;
        }
        
        [HttpGet()]
        public async Task<IActionResult> GetExportedMailboxes()
        {
            try
            {
                // Check if the folder exists
                if (!string.IsNullOrEmpty(_folderPath) && Directory.Exists(_folderPath))
                {
                    // Get directories in the folder
                    string[] directories = Directory.GetDirectories(_folderPath);

                    // Extract folder names
                    var folderNames = Array.ConvertAll(directories, Path.GetFileName);

                    var result = new
                    {
                        FolderPath = _folderPath,
                        DirectoryCount = directories.Length,
                        Directories = folderNames
                    };

                    return Ok(result);
                }
                else
                {
                    return NotFound("Folder not found or invalid folder path specified.");
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, "Internal Server Error");
            }
        }


    
            
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CredentialsDto user)
        {
            if (!string.IsNullOrEmpty(user.username) && Directory.Exists($"{_folderPath}/{user.username}"))
            {
                var job = await _redisDatabase.HashGetAsync(user.username, "status");
                if (job.HasValue)
                {
                    
                    return Ok(new
                    {
                        username = user.username,
                        status = job.ToString()
                    });
                }
                await  _redisQueue.EnqueueJobAsync(new JobModel()
                {
                    Username = user.username,
                });
                
                return Ok(new
                {
                    
                    username = user.username,
                    status = job.ToString()
                });
            }
            else
            {
                return NotFound("User data not found. Please contact admin if issue persists.");
            }

            
        }


        
    }

        
}

