using FileManagerApp.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Requests;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileManagerApp
{
    public class Program
    {
        /// <summary>
        /// The Drive API scopes.
        /// </summary>
        private static readonly string[] Scopes = new[] { DriveService.Scope.Drive };

        private static string ApplicationName = "Drive API .NET sismmar-filemanager"; //4/dy7Zz3pd18xuYcap8GdgEKsZOv541CaEKF0biUq92M8#

        public static void Main(string[] args)
        {
            Log("Welcome to Google Drive FileManager!");
            //Log("Type 't' to execute tests ");
            //var key = Console.ReadKey();
            //if (key.Key == ConsoleKey.T)
            //{
            //    var month = GetMonthFolder();
                
            //    Log($"\n{month}");
            //}

            try
            {
                var task = Task.Run(Run);
                task.Wait();
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Log("ERROR: " + e.Message);
                }
            }

            Log("Press any key to continue...");
            Console.ReadLine();
        }

        private static void Log(string message)
        {
            Console.WriteLine($"- {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} | { message }");
        }

        public static async Task Run()
        {
            GoogleWebAuthorizationBroker.Folder = "Drive.Sample";
            UserCredential credential = await GetCredentials();
            
            // Create the service.
            var driveService = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Drive API Sample",
            });

            // Create a folder if not exist
            driveService.CreateFolder("sismmar", "folder to storage some scripts!");

            // Get the latest file to Upload

            // Upload fo the current file
            // - find the sismmar folder
            // - find the current month folder (008-august, 009-september) inside the sismmar folder
            // - upload the current file inside the current month folder
            // done

            Console.Read();
        }

        public static async Task Go()
        {
            //notasecret
            //sismmar-sisges-d3f15fa8366e.p12

            string[] scopes = new string[] { DriveService.Scope.Drive }; // Full access
            var keyFilePath = @"C:\Users\m.lourenco\Downloads\sismmar-sisges-d3f15fa8366e.p12";    // Downloaded from https://console.developers.google.com 
            var serviceAccountEmail = "sismmar-filemanager@sismmar-sisges.iam.gserviceaccount.com";  // found https://console.developers.google.com 


            //loading the Key file 
            var certificate = new X509Certificate2(keyFilePath, "notasecret", X509KeyStorageFlags.Exportable);
            var credential = new ServiceAccountCredential(new ServiceAccountCredential.Initializer(serviceAccountEmail)
            {
                Scopes = scopes
            }.FromCertificate(certificate));

            // Create the service.
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Drive API Sample",
            });

            service.CreateFolder("sismmar");
            service.ListFiles();

            Console.Read();
        }        

        

        /// <summary>
        /// Uploads a file
        /// Documentation: https://developers.google.com/drive/v2/reference/files/insert
        /// </summary>
        /// <param name="_service">a Valid authenticated DriveService</param>
        /// <param name="_uploadFile">path to the file to upload</param>
        /// <param name="_parent">Collection of parent folders which contain this file. 
        ///                       Setting this field will put the file in all of the provided folders. root folder.</param>
        /// <returns>If upload succeeded returns the File resource of the uploaded file 
        ///          If the upload fails returns null</returns>
        public static File uploadFile(DriveService service, string uploadFilePath, string folderName)
        {
            var fileMetadata = new File()
            {
                Name = System.IO.Path.GetFileName(uploadFilePath)
            };
            FilesResource.CreateMediaUpload request;
            using (var stream = new System.IO.FileStream(uploadFilePath, System.IO.FileMode.Open))
            {
                request = service.Files.Create(fileMetadata, stream, "image/jpeg"); //"text/plain"
                request.Fields = "id";
                request.Upload();
            }
            var file = request.ResponseBody;
            Console.WriteLine("File ID: " + file.Id);

            return null;

        }

        private async static Task<UserCredential> GetCredentials()
        {
            var assembly = Assembly.GetEntryAssembly();
            var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(r => r.Contains("client_secret.json"));
            if (string.IsNullOrWhiteSpace(resourceName))
                throw new System.IO.FileNotFoundException($"The Resource was not found!");

            var resourceStream = assembly.GetManifestResourceStream(resourceName);
            var credPath = assembly.Location.Replace(assembly.ManifestModule.Name, string.Empty);
            credPath = System.IO.Path.Combine(credPath, "\\credentials\\google-drive-sismmar.json");
            
            // Get OAuth Credentials
            UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(resourceStream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true));

            return credential;
        }

        private static string GetMonthFolder()
        {
            return $"{DateTime.Now.Month.ToString("000")}-{DateTime.Now.ToString("MMMM")}";
        }
    }

    public static class DriveServiceExtension
    {
        public static void CreateFolder(this DriveService service, string folderName, string description = null)
        {
            var directory = new Google.Apis.Drive.v3.Data.File
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder",
                Description = description ?? string.Empty
            };

            var fileList = service.Files.List().Execute();
            var folderExists = fileList.Files.Any(f => f.MimeType == directory.MimeType && f.Name == directory.Name);
            if (folderExists)
            {
                Log($"Folder [{directory.Name}] already exist!");
            }
            else
            {
                var request = service.Files.Create(directory);
                request.Execute();
                Log($"Folder [{directory.Name}] was created!");
            }
        }

        public static IDirectResponseSchema GetFolder(this DriveService service, string folderName)
        {
            var directory = new Google.Apis.Drive.v3.Data.File
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder"
            };

            var fileList = service.Files.List().Execute();
            var folder = fileList.Files.FirstOrDefault(f => f.MimeType == directory.MimeType && f.Name == directory.Name);
            if (folder == null)
            {
                Log($"Folder [{directory.Name}] was not found!");
                return null;
            }
            else
            {
                Log($"Folder [{directory.Name}] was found!");
                return folder;
            }
        }

        ///<summary>
        /// List all of the files and directories for the current user.  
        /// 
        /// Documentation: https://developers.google.com/drive/v2/reference/files/list
        /// Documentation Search: https://developers.google.com/drive/web/search-parameters
        /// 
        ///a Valid authenticated DriveService        
        ///if Search is null will return all files        
        /// </summary>
        public static IEnumerable<IDirectResponseSchema> GetFiles(this DriveService service, string search)
        {
            var files = new List<IDirectResponseSchema>();
            try
            {
                //List all of the files and directories for the current user.  
                // Documentation: https://developers.google.com/drive/v2/reference/files/list
                FilesResource.ListRequest list = service.Files.List();
                list.PageSize = 1000;
                if (search != null)
                {
                    list.Q = search;
                }
                FileList filesFeed = list.Execute();

                //// Loop through until we arrive at an empty page
                while (filesFeed.Files != null)
                {
                    // Adding each item  to the list.
                    foreach (File item in filesFeed.Files)
                    {
                        files.Add(item);
                    }

                    // We will know we are on the last page when the next page token is
                    // null.
                    // If this is the case, break.
                    if (filesFeed.NextPageToken == null)
                    {
                        break;
                    }

                    // Prepare the next page of results
                    list.PageToken = filesFeed.NextPageToken;

                    // Execute and process the next page request
                    filesFeed = list.Execute();
                }
            }
            catch (Exception ex)
            {
                // In the event there is an error with the request.
                Log($"Ocorreu algum erro não tratado: {ex.Message}");
            }
            return files;
        }

        public static void ListFiles(this DriveService service)
        {
            var request = service.Files.List();
            request.Fields = "files(id, name)";
            var result = request.Execute();
            var files = result.Files;
            if (files != null && files.Any())
            {
                foreach (var file in files)
                {
                    Log($"{file.Name} ({file.Id})");
                }
            }
            else
            {
                Log("No files found.");
            }
        }

        public static void ListFilesByFolder(this DriveService service, string folderName)
        {
            var request = service.Files.List();
            request.Fields = "files(id, name)";
            var result = request.Execute();
            var files = result.Files;
            if (files != null && files.Any())
            {
                foreach (var file in files)
                {
                    Log($"{file.Name} ({file.Id})");
                }
            }
            else
            {
                Log("No files found.");
            }
        }

        private static void Log(string message)
        {
            Console.WriteLine($"- {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} | { message }");
        }
    }
}