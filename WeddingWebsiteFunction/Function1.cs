using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using System.Collections.Generic;
using Azure.Storage.Blobs.Models;
using Azure.Data.Tables;
using System.Linq;

namespace TestingSAS
{
    public static class Function1
    {
        [FunctionName("GetUploadBlobSAS")]
        public static IActionResult GetUploadBlobSAS(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string connectionString = System.Environment.GetEnvironmentVariable("StorageConnectionString");

            string containerName = "weddingphotoscontainertest1";

            string blobName = System.Guid.NewGuid().ToString(); // Generate a new unique file name

            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            BlobSasBuilder sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerClient.Name,
                BlobName = blobClient.Name,
                Resource = "b"
            };
            sasBuilder.StartsOn = DateTimeOffset.UtcNow;
            sasBuilder.ExpiresOn = DateTimeOffset.UtcNow.AddHours(1);
            sasBuilder.SetPermissions(BlobSasPermissions.Write);

            Uri sasUri = blobClient.GenerateSasUri(sasBuilder);

            return new OkObjectResult(sasUri);
        }
        [FunctionName("GetBrowseContainerSAS")]
        public static IActionResult GetBrowseContainerSAS(
                [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
                ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string connectionString = System.Environment.GetEnvironmentVariable("StorageConnectionString");

            string containerName = "weddingphotoscontainertest1";

            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            BlobSasBuilder sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerClient.Name,
                Resource = "c"
            };
            sasBuilder.StartsOn = DateTimeOffset.UtcNow;
            sasBuilder.ExpiresOn = DateTimeOffset.UtcNow.AddHours(1);
            sasBuilder.SetPermissions(BlobSasPermissions.List | BlobSasPermissions.Read);

            Uri sasUri = containerClient.GenerateSasUri(sasBuilder);

            return new OkObjectResult(sasUri);
        }

        [FunctionName("TableQuery")]
        public static IActionResult TableQuery(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            string connectionString = System.Environment.GetEnvironmentVariable("StorageConnectionString");
            // optional variables
            string order = req.Query.ContainsKey("order") ? req.Query["order"] : "asc";
            int page;
            int size;

            // check for page and size, if not passed gets all
            if (!int.TryParse(req.Query["page"], out page)) { page = 1; };
            if (!int.TryParse(req.Query["size"], out size)) { size = int.MaxValue; }

            // connect to table
            var client = new TableServiceClient(connectionString);
            var tableClient = client.GetTableClient("weddingphotostabletest1");

            // queries table
            var entities = tableClient.Query<Azure.Data.Tables.TableEntity>();

            // gets the page were looking for
            int recordsToSkip = (page - 1) * size;
            var correctPage = entities.OrderBy((x) => order.ToLower() == "desc" ? -Int64.Parse((string)x["PartitionKey"]) : Int64.Parse((string)x["PartitionKey"])).Skip(recordsToSkip).Take(size).ToList();

            var jsonList = new List<string>();

            // this foreach is the overinformed object, convert to the object we want
            foreach (var record in correctPage)
            {

                jsonList.Add(JsonConvert.SerializeObject(new MyTableEntity()
                {
                    PartitionKey = (string)record["PartitionKey"],
                    RowKey = (string)record["RowKey"],
                    fullSizeUrl = (string)record["fullSizeUrl"],
                    thumbnailUrl = (string)record["thumbnailUrl"],
                    name = (string)record["name"]
                }));

            }

            return jsonList.Count < 1 ? new NoContentResult() : new OkObjectResult(jsonList);
        }

        //Thumnail and table query

        [FunctionName("TableAndThumnail")]
        [return: Microsoft.Azure.WebJobs.Table("weddingphotostabletest1")]
        public static async Task<MyTableEntity> Run([BlobTrigger("weddingphotoscontainertest1/{name}", Connection = "StorageConnectionString")] Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
            string connectionString = System.Environment.GetEnvironmentVariable("StorageConnectionString");
            string originalBlobUrl = "https://weddingphotoscr.blob.core.windows.net/weddingphotoscontainertest1/" + name;

            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("weddingphotoscontainertest1");
            BlobClient originalBlobClient = containerClient.GetBlobClient(name);
            BlobProperties properties = originalBlobClient.GetProperties();
            string userName = properties.Metadata.ContainsKey("Name") ? properties.Metadata["Name"] : "";

            // generate thumbnail
            string AICredential = System.Environment.GetEnvironmentVariable("AI_ConnString");
            ApiKeyServiceClientCredentials credentials = new ApiKeyServiceClientCredentials(AICredential);
            var visionClient = new ComputerVisionClient(credentials) { Endpoint = "https://weddingwebsiteaiservice.cognitiveservices.azure.com/" };

            // put our image into memory stream
            var thumbnailStream = await visionClient.GenerateThumbnailInStreamAsync(800, 800, myBlob, true);//new MemoryStream(memoryStream.ToArray()), true);

            // add to thumbnail container
            string thumbnailBlobName = Guid.NewGuid().ToString();            
            BlobClient blobClient = new BlobClient(connectionString, "thumbnail-imagestest1", thumbnailBlobName);

            await blobClient.UploadAsync(thumbnailStream, true);

            // add to table
            // Create an instance of the table entity
            var tableEntity = new MyTableEntity
            {
                PartitionKey = DateTime.Now.ToString("yyyyMMddHHmmss"),
                RowKey = thumbnailBlobName,
                fullSizeUrl = originalBlobUrl,
                thumbnailUrl = blobClient.Uri.ToString(),
                name = userName
            };

            return tableEntity;
        }
    }
}

