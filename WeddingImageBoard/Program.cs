using System;
using System.Net.Http;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using WeddingImageBoard;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// key vault settings
//var keyVaultUri = new Uri("https://weddingwebsitekeyvault.vault.azure.net/");
//var secretClient = new SecretClient(keyVaultUri, new DefaultAzureCredential());

//try
//{
//    var secretResponse = await secretClient.GetSecretAsync("weddingwebsitekey");
//    var storageKey = secretResponse.Value.Value;
//    builder.Configuration["weddingwebsitekey"] = storageKey;
//}

//catch(Exception ex)
//{
//    Console.WriteLine(ex.Message);
//}

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
