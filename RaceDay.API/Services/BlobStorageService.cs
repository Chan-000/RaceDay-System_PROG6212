using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.API.Services
{
    public class BlobStorageService
    {
        private readonly BlobContainerClient _containerClient;

        private const long MaxFileSize = 5 * 1024 * 1024; 

        public BlobStorageService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureBlobStorage:ConnectionString"];
            var containerName = configuration["AzureBlobStorage:ContainerName"];

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Azure Blob Storage connection string is missing.");
            }

            if (string.IsNullOrWhiteSpace(containerName))
            {
                throw new InvalidOperationException(
                    "Azure Blob Storage container name is missing.");
            }

            _containerClient = new BlobContainerClient(connectionString, containerName);
        }

        public async Task<string> UploadAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("Please select an image to upload.");
            }

            if (file.Length > MaxFileSize)
            {
                throw new ArgumentException("The image must not exceed 5 MB.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            var allowedTypes = new Dictionary<string, string>
            {
                { ".jpg", "image/jpeg" },
                { ".jpeg", "image/jpeg" },
                { ".png", "image/png" },
                { ".gif", "image/gif" },
                { ".webp", "image/webp" }
            };

            if (!allowedTypes.ContainsKey(extension))
            {
                throw new ArgumentException(
                    "Only JPG, JPEG, PNG, GIF and WEBP images are allowed.");
            }

            var contentType = allowedTypes[extension];

            if (!string.Equals(file.ContentType, contentType,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("The selected file is not a valid image type.");
            }

            await _containerClient.CreateIfNotExistsAsync();

            var blobName = folder + "/" + Guid.NewGuid().ToString("N") + extension;

            var blobClient = _containerClient.GetBlobClient(blobName);

            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = contentType
                    }
                });
            }

            return blobClient.Uri.ToString();
        }
    }
}
