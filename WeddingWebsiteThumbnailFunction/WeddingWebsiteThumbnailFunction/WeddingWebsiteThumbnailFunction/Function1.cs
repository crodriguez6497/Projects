using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage;
using Azure;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace WeddingWebsiteThumbnailFunction
{
    public class Function1
    {
        [FunctionName("Function1")]
        [return: Microsoft.Azure.WebJobs.Table("WeddingStuff", Connection = "StorageConnectionString")]
        public static async Task<MyTableEntity> Run([BlobTrigger("ORIGINAL_CONTAINER_NAME/{name}", Connection = "StorageConnectionString")]Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            // Get the original blob URL
            string originalBlobUrl = "FULL_URL_TO_CONTAINER" + name;

            // get original blob properties so you can get the name from metadata
            BlobServiceClient blobServiceClient = new BlobServiceClient("STORAGE_CONNECTION_STRING");
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("ORIGINAL_CONTAINER_NAME");
            BlobClient originalBlobClient = containerClient.GetBlobClient(name);
            BlobProperties properties = originalBlobClient.GetProperties();

            // ========== THIS IS FOR TESTING DO NOT USE ==========
            if (!properties.Metadata.ContainsKey("Name")) {

                var metadata = new Dictionary<string, string>
                {
                    { "Name", "Bobbi" },
                };

                originalBlobClient.SetMetadata(metadata);
            }
            // ========== END TESTING ==========

            // if name in properties add name else dont
            string userName = properties.Metadata.ContainsKey("Name") ? properties.Metadata["Name"] : "";

            // generate thumbnail
            // required to access vision service
            ApiKeyServiceClientCredentials credentials = new ApiKeyServiceClientCredentials("AI_KEY");
            var visionClient = new ComputerVisionClient(credentials) { Endpoint = "AI_ENDPOINT" };

            // put our image into memory stream
            var thumbnailStream = await visionClient.GenerateThumbnailInStreamAsync(800, 800, myBlob, true);//new MemoryStream(memoryStream.ToArray()), true);

            // add to thumbnail container
            string blobName = Guid.NewGuid().ToString();

            BlobClient blobClient = new BlobClient("STORAGE_CONNECTION_STRING", "thumbnail-images", blobName);

            await blobClient.UploadAsync(thumbnailStream, true);

            // add to table
            // Create an instance of the table entity
            var tableEntity = new MyTableEntity {
                PartitionKey = DateTime.Now.ToString("yyyyMMddHHmmss"),
                RowKey = blobName,
                fullSizeUrl = originalBlobUrl,
                thumbnailUrl = blobClient.Uri.ToString(),
                name = userName
            };

            return tableEntity;
        }
    }
}
