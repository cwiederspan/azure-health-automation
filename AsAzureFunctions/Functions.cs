using System;
using System.Net;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.Management.FrontDoor;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;

namespace AsAzureFunctions {

    public static class MyFunctions {

        // Read in the settings from the Environment
        private static string SSAS_DB_NAME = Environment.GetEnvironmentVariable("SSAS_DB_NAME");
        private static string SSAS_REGION = Environment.GetEnvironmentVariable("SSAS_REGION");
        private static string IDENTITY_CONN_STR = Environment.GetEnvironmentVariable("IDENTITY_CONN_STRING");
        private static string SUBSCRIPTION_ID = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID");
        private static string RESOURCE_GROUP_NAME = Environment.GetEnvironmentVariable("RESOURCE_GROUP_NAME");
        private static string FRONT_DOOR_NAME = Environment.GetEnvironmentVariable("FRONT_DOOR_NAME");
        private static string BACKEND_POOL_NAME = Environment.GetEnvironmentVariable("BACKEND_POOL_NAME");
        private static string BACKEND_ADDRESS = Environment.GetEnvironmentVariable("BACKEND_ADDRESS");

        [FunctionName("location")]
        public static HttpResponseMessage GetLocation(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "server/location")] HttpRequest req,
            ExecutionContext context,
            ILogger log
        ) {
            
            log.LogInformation("GetLocation method has been triggered...");
            
            var server = $"asazure://{SSAS_REGION}.asazure.windows.net/{SSAS_DB_NAME}";

            var response = new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(server)
            };
            return response;
        }

        [FunctionName("timer")]
        public static async Task CheckHealthTimerAsync(
            [TimerTrigger("0 */1 * * * *")]TimerInfo timer, 
            ILogger log
        ) {
            
            log.LogInformation("CheckHealthTimerAsync method has been triggered...");

            // Check the status of the Azure Resource
            var status = await CheckHealthAsync(log);

            // Handle the Firewall
            await UpdateFrontDoorAsync(status.IsHealthy, log);
        }

        [FunctionName("health")]
        public static async Task<HttpResponseMessage> CheckHealthHttpAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "server/health")] HttpRequest req,
            ExecutionContext context,
            ILogger log
        ) {
            
            log.LogInformation("CheckHealthHttpAsync method has been triggered...");

            // Check the status of the Azure Resource
            var status = await CheckHealthAsync(log);

            // Handle the Firewall
            await UpdateFrontDoorAsync(status.IsHealthy, log);

            // Return the result
            return new HttpResponseMessage() {
                StatusCode = status.IsHealthy ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable,
                Content = new StringContent(status.Message)
            };
        }

        private static async Task<HealthStatus> CheckHealthAsync(ILogger log) {
            
            log.LogInformation("CheckHealthAsync method has been triggered...");

            // Get a token to access the Azure Resource Health API using the managed identity
            var tokenProvider = new AzureServiceTokenProvider(IDENTITY_CONN_STR);
            var token = await tokenProvider.GetAccessTokenAsync("https://management.azure.com/");
            var ssasHealthCheckUrl = $"https://management.azure.com/subscriptions/{SUBSCRIPTION_ID}/resourceGroups/{RESOURCE_GROUP_NAME}/providers/Microsoft.AnalysisServices/servers/{SSAS_DB_NAME}/providers/Microsoft.ResourceHealth/availabilityStatuses/current?api-version=2017-07-01";

            // Use the Azure Resource Health API to get the status health for the server
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var json = await client.GetStringAsync(ssasHealthCheckUrl);
            var result = JsonSerializer.Deserialize<HealthResponse>(json);

            // Return the result
            return new HealthStatus {
                Status = result.properties.availabilityState,
                Message = result.properties.summary
            };
        }

        private static async Task UpdateFrontDoorAsync(bool isHealthy, ILogger log) {

            log.LogInformation("UpdateFrontDoorAsync method has been triggered...");

            // Get a token to access the Azure Resource Health API using the managed identity
            var tokenProvider = new AzureServiceTokenProvider(IDENTITY_CONN_STR);
            var token = await tokenProvider.GetAccessTokenAsync("https://management.azure.com/");
            var credentials = new TokenCredentials(token);
            
            var mgmtClient = new FrontDoorManagementClient(credentials) {
                SubscriptionId = SUBSCRIPTION_ID
            };

            // Use the Azure Management SDK to get the Front Door
            var frontDoor = await mgmtClient.FrontDoors.GetAsync(RESOURCE_GROUP_NAME, FRONT_DOOR_NAME);

            // Find the associated Backend
            var backend = frontDoor.BackendPools
                .Single(p => p.Name == BACKEND_POOL_NAME)
                .Backends.Single(s => s.Address == BACKEND_ADDRESS);

            // Set the status of the front door backend
            var newStatus = isHealthy ? "Enabled" : "Disabled";
            
            // If the status has changed, then update the status
            if (backend.EnabledState != newStatus) {
                backend.EnabledState = newStatus;
                var result = await mgmtClient.FrontDoors.CreateOrUpdateAsync(RESOURCE_GROUP_NAME, FRONT_DOOR_NAME, frontDoor);
            }
        }

        private class HealthStatus {

            public string Status { get; set; }

            public string Message { get; set; }

            public bool IsHealthy {
                get {
                    return this.Status.ToLower() == "available";
                }
            }
        }
            
        private class HealthResponse {

            public string id { get; set; }

            public string name { get; set; }

            public string type { get; set; }

            public string location { get; set; }

            public HealthResponseProperties properties { get; set; }
        }

        private class HealthResponseProperties {

            public string availabilityState { get; set; }

            public string title { get; set; }

            public string summary { get; set; }

            public string reasonType { get; set; }

            public DateTime rootCauseAttributionTime { get; set; }

            public DateTime occuredTime { get; set; }

            public string reasonChronicity { get; set; }

            public DateTime reportedTime { get; set; }

            public DateTime resolutionETA { get; set; }
        }
    }
}