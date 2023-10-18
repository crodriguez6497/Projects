using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Collections.Generic;

namespace WeddingWebsiteThumbnailFunction
{
    public class Function1
    {
        [FunctionName("Function1")]
        [return: Microsoft.Azure.WebJobs.Table("WeddingStuff", Connection = "StorageConnectionString")]
        public static async Task<MyTableEntity> Run([BlobTrigger("CONTAINER_NAME/{name}", Connection = "StorageConnectionString")]Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            // Get the original blob URL
            string originalBlobUrl = "https://ai102form2287314696.blob.core.windows.net/margies-images/" + name;

            //string connectionString = "your_connection_string";
            //BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);

            //string containerName = "your_container_name";
            //string blobName = "your_blob_name";

            //BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            //BlobClient blobClient = containerClient.GetBlobClient(blobName);

            //BlobProperties properties = blobClient.GetProperties();

            //string userName = "";
            //// if name in properties add name else dont
            //if (properties.Metadata.ContainsKey("userName")) {
            //    userName = properties.Metadata["your_metadata_key"];
            //}

            // generate thumbnail
            // required to access vision service
            ApiKeyServiceClientCredentials credentials = new ApiKeyServiceClientCredentials("AI_CREDENTIAL");
            var visionClient = new ComputerVisionClient(credentials) { Endpoint = "https://multiaiservices-xh1.cognitiveservices.azure.com/" };

            // put our image into memory stream
            var thumbnailStream = await visionClient.GenerateThumbnailInStreamAsync(800, 800, myBlob, true);//new MemoryStream(memoryStream.ToArray()), true);

            // add to thumbnail container
            string blobName = Guid.NewGuid().ToString();

            BlobClient blobClient = new BlobClient("STORAGE_CREDENTIAL", "thumbnail-images", blobName);

            await blobClient.UploadAsync(thumbnailStream, true);

            // add to table
            // Create an instance of the table entity
            var tableEntity = new MyTableEntity {
                PartitionKey = DateTime.Now.ToShortDateString(),
                RowKey = blobName,
                fullSizeUrl = originalBlobUrl,
                thumbnailUrl = blobClient.Uri.ToString(),
                name = name
            };

            return tableEntity;
        }
    }
}
