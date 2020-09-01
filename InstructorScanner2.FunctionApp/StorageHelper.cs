using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace InstructorScanner2.FunctionApp
{
    public interface IStorageHelper
    {
        Task<bool> FileExistsAsync(string containerName, string fileName, CancellationToken cancellationToken = default(CancellationToken));
        Task<string> ReadFileAsync(string containerName, string fileName, CancellationToken cancellationToken = default(CancellationToken));
        Task SaveFileAsync(string containerName, string fileName, string fileContents, CancellationToken cancellationToken = default(CancellationToken));
    }

    public class StorageHelper : IStorageHelper
    {
        private readonly IOptions<AppSettings> _appSettings;
        private readonly Lazy<CloudBlobClient> _cloudBlobClient;

        public StorageHelper(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings;

            _cloudBlobClient = new Lazy<CloudBlobClient>(() =>
            {
                var cloudStorageAccount = CloudStorageAccount.Parse(_appSettings.Value.StorageConnectionString);
                return cloudStorageAccount.CreateCloudBlobClient();
            });

        }

        public async Task<bool> FileExistsAsync(string containerName, string fileName, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await _cloudBlobClient
                .Value
                .GetContainerReference(containerName)
                .GetBlockBlobReference(fileName)
                .ExistsAsync();
        }

        public async Task<string> ReadFileAsync(string containerName, string fileName, CancellationToken cancellationToken = default(CancellationToken))
        {
            var cloudBlobContainer = _cloudBlobClient.Value.GetContainerReference(containerName);

            var blockBlob = cloudBlobContainer.GetBlockBlobReference(fileName);
            return await blockBlob.DownloadTextAsync();
        }

        public async Task SaveFileAsync(string containerName, string fileName, string fileContents, CancellationToken cancellationToken = default(CancellationToken))
        {
            var cloudBlockBlob = _cloudBlobClient.Value.GetContainerReference(containerName).GetBlockBlobReference(fileName);

            if(Path.GetExtension(fileName) == ".html")
            {
                cloudBlockBlob.Properties.ContentType = "text/html";
                await cloudBlockBlob.SetPropertiesAsync();
            }

            await cloudBlockBlob.UploadTextAsync(fileContents);
        }
    }
}
