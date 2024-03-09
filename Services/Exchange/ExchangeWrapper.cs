using Microsoft.Exchange.WebServices.Data;
using Microsoft.Identity.Client;
using MigrateClient.Interfaces;
using MigrateClient.Interfaces.Exchange;
using MigrateClient.Interfaces.Updates;
using SysTask = System.Threading.Tasks.Task;
using Task = Microsoft.Exchange.WebServices.Data.Task;

namespace MigrateClient.Services.Exchange
{
    public class ExchangeWrapper : IExchangeWrapper
    {
        private readonly ILogger<ExchangeWrapper> _logger;
        private readonly ExchangeService _ewsService;
        private static readonly string FolderPath = "/home/mailboxes/mailbox-extracts/";
        private static readonly Dictionary<string, WellKnownFolderName> FolderMappings = new Dictionary<string, WellKnownFolderName>(StringComparer.OrdinalIgnoreCase)
        {
            { "Trash", WellKnownFolderName.DeletedItems },
            { "Sent", WellKnownFolderName.SentItems },
            { "Junk", WellKnownFolderName.JunkEmail}
        };

        private readonly IQueueService _queue;
        private readonly IConfidentialClientApplication tokenAcquisition = ConfidentialClientApplicationBuilder
                   .Create(Environment.GetEnvironmentVariable("OFFICE_365_CLIENT_ID"))
                   .WithClientSecret(Environment.GetEnvironmentVariable("OFFICE_365_CLIENT_SECRET"))
                   .WithTenantId(Environment.GetEnvironmentVariable("OFFICE_365_TENANT_ID"))
                   .Build();
        private AuthenticationResult authResult;
        public SysTask Initialization { get; private set; }

        public ExchangeWrapper(ILogger<ExchangeWrapper> logger, IQueueService queue)
        {
            _logger = logger;
            Initialization = InitializeAsync();
            _ewsService = new ExchangeService();
            _queue = queue;
        }

        private async SysTask InitializeAsync()
        {
            // Asynchronously initialize this instance.
            var ewsScopes = new string[] { "https://outlook.office365.com/.default" };
            authResult = await tokenAcquisition.AcquireTokenForClient(ewsScopes)
                        .ExecuteAsync();
            _ewsService.Url = new Uri("https://outlook.office365.com/EWS/Exchange.asmx");
            _ewsService.Credentials = new OAuthCredentials(authResult.AccessToken);

        }

        public async SysTask ImportEmails(string username)  
        {
            try
            {
                _ewsService.ImpersonatedUserId = new ImpersonatedUserId(ConnectingIdType.SmtpAddress, username);
                if (!_ewsService.HttpHeaders.ContainsKey("X-AnchorMailbox"))
                {
                    _ewsService.HttpHeaders.Add("X-AnchorMailbox",username);
                }

            } catch (Exception ex)
            {
                await Error(ex, username);
            }
           
            await SysTask.Run(()=>MigrateMailbox(username));
        }


        #region Helpers

        private async SysTask MigrateMailbox(string path)
        {
            try
            {
                // Loop through all folders of the mailbox and create email messages
                foreach (string folder in GetAllFolders(path))
                {
                    string[] emailsFilesInFolder = Directory.GetFiles($"{FolderPath}/{path}/{folder}", "*eml");
                    
                    // Create a list of all emails for batch import
                    List<EmailMessage> emailsToBeImported = new List<EmailMessage>();

                    // Loop through all email files and read bytes
                    await Parallel.ForEachAsync(emailsFilesInFolder, async (emlFile, token)  =>
                    {
                        // Create new email type to hold email files
                        EmailMessage email = new(_ewsService);
                        await using (FileStream fs = new(emlFile, FileMode.Open, FileAccess.Read))
                        {
                            // Read the content of the email file
                            byte[] bytes = await File.ReadAllBytesAsync(emlFile);

                            // Set the contents of the .eml file to the MimeContent property.
                            email.MimeContent = new MimeContent("UTF-8", bytes);
                        }

                        // Indicate that this email is not a draft. Otherwise, the email will appear as a 
                        // draft to clients.
                        ExtendedPropertyDefinition PR_MESSAGE_FLAGS_msgflag_read = new ExtendedPropertyDefinition(3591, MapiPropertyType.Integer);
                        email.SetExtendedProperty(PR_MESSAGE_FLAGS_msgflag_read, 1);

                        // Add the read email to the email list
                        emailsToBeImported.Add(email);
                    });

                    
                    // Check if the folder is a default folder or not
                    if (Enum.TryParse(folder, true, out WellKnownFolderName mailFolder))
                    {
                        await SysTask.Run(() => _ewsService.CreateItems(
                            emailsToBeImported, mailFolder, MessageDisposition.SaveOnly, null));

                    }
                    else if (FolderMappings.TryGetValue(folder, out WellKnownFolderName wellKnownFolder))
                    {
                        
                        await SysTask.Run(() => _ewsService.CreateItems(
                            emailsToBeImported, wellKnownFolder, MessageDisposition.SaveOnly, null));
                    }
                    else
                    {
                        // Create custom folder and save the email
                        FolderId customFolder = await CreateCustomFolder(folder);
                        await SysTask.Run(()=>_ewsService.CreateItems(emailsToBeImported, customFolder, MessageDisposition.SaveOnly, null));
                   
                    }
                }
            } catch (Exception ex)
            {
                await Error(ex, path);
            }
     

        }
        private static  List<string> GetAllFolders(string username)
        {
            // Get a list of all folders in the specified path
            string[] subdirectories = Directory.GetDirectories($"{FolderPath}/{username}");
            List<string> folderNames = subdirectories.Select(subdir => Path.GetFileName(subdir)).ToList();
            folderNames.Remove("Emailed Contacts");
            return folderNames;
        }

      
        private async Task<FolderId> CreateCustomFolder(string folderName)
        {
            FolderView folderView = new FolderView(1);
            folderView.PropertySet = new PropertySet(BasePropertySet.IdOnly);
            folderView.Traversal = FolderTraversal.Shallow;
            SearchFilter searchFilter = new SearchFilter.IsEqualTo(FolderSchema.DisplayName, folderName);
            FindFoldersResults findFolderResults = await SysTask.Run(
                () => _ewsService.FindFolders(WellKnownFolderName.MsgFolderRoot, searchFilter, folderView)); 

            if (findFolderResults.Folders.Count > 0)
            {
                return findFolderResults.Folders.First().Id;

            }
            else
            {
                // Folder doesn't exist, create a new one
                Folder folder = new Folder(_ewsService);
                folder.DisplayName = folderName;
                await SysTask.Run(() => folder.Save(WellKnownFolderName.MsgFolderRoot));
				_logger.LogCritical(folder.Id.ToString() + "IDs");
                return folder.Id;
            }
        }


        private async SysTask DeleteFolder(string username)
        {
            
        }

        private async SysTask Error(Exception ex, string username)
        {
            await _queue.UpdateJobStatusAsync(username, "FAILURE", ex.Message);
            _logger.LogError($"An error occured {ex}");
        }
        #endregion



    }
}
