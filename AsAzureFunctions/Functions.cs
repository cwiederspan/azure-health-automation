using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;

namespace AsAzureFunctions {

    public static class MyFunctions {

        private static HttpClient Client = new HttpClient();

        private static string FILENAME = "/mnt/data/availability/error";

        [FunctionName("location")]
        public static HttpResponseMessage GetLocation(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "server/location")] HttpRequest req,
            ExecutionContext context,
            ILogger log
        ) {
            
            log.LogInformation("GetLocation method has been triggered...");

            var server = Environment.GetEnvironmentVariable("SSAS_SERVER_NAME");

            var response = new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(server)
            };
            return response;
        }
        
        /*
        [FunctionName("health")]
        public static async Task<HttpResponseMessage> GetHealthAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "head", Route = "server/health")] HttpRequest req,
            ExecutionContext context,
            ILogger log
        ) {
            
            log.LogInformation("GetHealthAsync method has been triggered...");

            // Get the health check URL from the settings
            var url = Environment.GetEnvironmentVariable("HEALTH_CHECK_URL");
            var idConnStr = Environment.GetEnvironmentVariable("IDENTITY_CONN_STRING");

            // Get a token to access the Azure Resource Health API using the managed identity
            var tokenProvider = new AzureServiceTokenProvider(idConnStr);
            var token = await tokenProvider.GetAccessTokenAsync("https://management.azure.com/");

            // Use the Azure Resource Health API to get the status health for the server
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var json = await Client.GetStringAsync(url);
            var result = JsonConvert.DeserializeObject<HealthResponse>(json);

            // Check the result
            var status = HttpStatusCode.ServiceUnavailable;
            if (result.properties.availabilityState.ToLower() == "available") {
                status = HttpStatusCode.OK;
            }

            // Set the status message
            var message = $"{result.properties.availabilityState}: {result.properties.summary}";

            // Return the result
            var response = new HttpResponseMessage(status) {
                Content = new StringContent(message)
            };
            return response;
        }
        */

        [FunctionName("health")]
        public static async Task<HttpResponseMessage> GetHealthAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "head", Route = "server/health")] HttpRequest req,
            ExecutionContext context,
            ILogger log
        ) {
            
            log.LogInformation("GetHealthAsync method has been triggered...");

            var status = HttpStatusCode.OK;
            var message = String.Empty;
            if (File.Exists(FILENAME) == true) {
                message = await File.ReadAllTextAsync(FILENAME);
                status = HttpStatusCode.ServiceUnavailable;
            }

            // Return the result
            var response = new HttpResponseMessage(status) {
                Content = new StringContent(message)
            };
            return response;
        }

        [FunctionName("timer")]
        public static async Task CheckHealthTimerAsync(
            [TimerTrigger("0 */1 * * * *")]TimerInfo timer, 
            ILogger log
        ) {
            
            log.LogInformation("CheckHealthTimerAsync method has been triggered...");

            await CheckHealthAsync(log);
        }

        [FunctionName("check")]
        public static Task<HttpResponseMessage> CheckHealthHttpAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "server/health/check")] HttpRequest req,
            ExecutionContext context,
            ILogger log
        ) {
            
            log.LogInformation("CheckHealthHttpAsync method has been triggered...");

            return CheckHealthAsync(log);
        }

        private static async Task<HttpResponseMessage> CheckHealthAsync(
            ILogger log
        ) {
            
            log.LogInformation("CheckHealthAsync method has been triggered...");

            // Get the health check URL from the settings
            var url = Environment.GetEnvironmentVariable("HEALTH_CHECK_URL");
            var idConnStr = Environment.GetEnvironmentVariable("IDENTITY_CONN_STRING");

            // Get a token to access the Azure Resource Health API using the managed identity
            var tokenProvider = new AzureServiceTokenProvider(idConnStr);
            var token = await tokenProvider.GetAccessTokenAsync("https://management.azure.com/");

            // Use the Azure Resource Health API to get the status health for the server
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var json = await Client.GetStringAsync(url);
            var result = JsonConvert.DeserializeObject<HealthResponse>(json);

            // Check the result
            var status = HttpStatusCode.ServiceUnavailable;
            if (result.properties.availabilityState.ToLower() == "available") {
                status = HttpStatusCode.OK;
            }

            // DEBUG
            //result.properties.availabilityState = "Unavailable";

            // Set the status message
            var message = $"{result.properties.availabilityState}: {result.properties.summary}";

            // Check the result and if there's an error, write a file to storage
            if (result.properties.availabilityState.ToLower() != "available") {
                using (var file = File.CreateText(FILENAME)) {
                    await file.WriteLineAsync(message);
                }
            }
            else if (File.Exists(FILENAME) == true) {
                File.Delete(FILENAME);
            }

            // Return the result
            var response = new HttpResponseMessage(status) {
                Content = new StringContent(message)
            };
            return response;
        }
    }
}