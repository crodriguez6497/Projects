using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Data.Tables;
using WeddingWebsiteThumbnailFunction;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

namespace FunctionApp2
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // optional variables
            string order = req.Query.ContainsKey("order") ? req.Query["order"] : "asc";
            int page;
            int size;

            // check for page and size, if not passed gets all
            if (!int.TryParse(req.Query["page"], out page)) { page = 1; };
            if (!int.TryParse(req.Query["size"], out size)) { size = int.MaxValue; }

            // connect to table
            var client = new TableServiceClient("STORAGE_CONNECTION_STRING");
            var tableClient = client.GetTableClient("TABLE_NAME");

            // queries table
            var entities = tableClient.Query<Azure.Data.Tables.TableEntity>();

            // gets the page were looking for
            int recordsToSkip = (page - 1) * size;
            var correctPage = entities.Skip(recordsToSkip).Take(size).OrderBy((x) => order.ToLower() == "dsc" ? -Int64.Parse((string)x["PartitionKey"]) : Int64.Parse((string)x["PartitionKey"]) ).ToList();

            var jsonList = new List<string>();

            // this foreach is the overinformed object, convert to the object we want
            foreach (var record in correctPage) {

                jsonList.Add(JsonConvert.SerializeObject(new MyTableEntity() {
                    PartitionKey = (string)record["PartitionKey"],
                    RowKey = (string)record["RowKey"],
                    fullSizeUrl = (string)record["fullSizeUrl"],
                    thumbnailUrl = (string)record["thumbnailUrl"],
                    name = (string)record["name"]
                }));

            }

            return jsonList.Count < 1 ? new NoContentResult() : new OkObjectResult(jsonList);
        }
    }
}
