using Microsoft.AspNetCore.Mvc;
using MigrateClient.Data.DTOs;
using MigrateClient.Data.Models;
using MigrateClient.Interfaces;
using MigrateClient.Interfaces.Exchange;
using StackExchange.Redis;


namespace MigrateClient.Controllers
{
    [Route("api/Migrate")]
    [ApiController]
    public class ImportController : ControllerBase
    {
        private readonly IExchangeWrapper _ewsClient;
        public static readonly string _folderPath = "/home/mailboxes/mailbox-extracts/";
        private readonly IQueueService _redisQueue;
        private readonly IDatabase _redisDatabase;
        public ImportController(IExchangeWrapper ewsClient, IQueueService queue, IConnectionMultiplexer multiplexer) //IBackgroundTaskQueue taskQueue)
        {
            _ewsClient = ewsClient;
            // _backgroundTaskQueue = taskQueue;
            _redisQueue = queue;
            _redisDatabase = multiplexer.GetDatabase();
        }
        
        [HttpGet]
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


        [HttpGet("All Tasks")]
        public async Task<List<JobModel>> Jobs()
        {
            return await _redisQueue.GetJobsAsync();
        }
        [HttpGet("Tasks in Queue")]
        public async Task<List<JobModel>> JobsInQueue()
        {
            return await _redisQueue.GetJobsInQueueAsync();
        }
            
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CredentialsDto user)
        {
            if (!string.IsNullOrEmpty(user.username) && Directory.Exists($"{_folderPath}/{user.username}"))
            {
                var job = await _redisDatabase.HashGetAsync(user.username, "status");
                if (job.HasValue)
                {
                    
                    return BadRequest(
                        $"User with id {user.username} done with migration. Last status is ${job.ToString()}." +
                        $"contact admin for extra assistance."
                        );
                }
                await  _redisQueue.EnqueueJobAsync(new JobModel()
                {
                    Username = user.username,
                });
            }
            else
            {
                return NotFound("User data not found. Please contact admin if issue persists.");
            }

            return Ok();
        }
    }

        
}

