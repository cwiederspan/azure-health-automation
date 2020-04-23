using System;
using System.Collections.Generic;
using System.Text;

namespace AsAzureFunctions {

/*

// Available
{
    "id": "/subscriptions/b9c770d1-cde9-4da3-ae40-95ce1a4fac0c/resourcegroups/cdw-analysisservices-20200401/providers/microsoft.analysisservices/servers/cdwanalysisservices20200401/providers/Microsoft.ResourceHealth/availabilityStatuses/current",
    "name": "current",
    "type": "Microsoft.ResourceHealth/AvailabilityStatuses",
    "location": "westus2",
    "properties": {
        "availabilityState": "Available",
        "title": "Available",
        "summary": "This server is running normally. There aren't any known Azure platform problems affecting this Analysis Services server.",
        "reasonType": "",
        "occuredTime": "2020-04-20T13:11:38Z",
        "reasonChronicity": "Transient",
        "reportedTime": "2020-04-20T13:13:50.7932991Z"
    }
}


// Unavailable
{
    "id": "/subscriptions/b9c770d1-cde9-4da3-ae40-95ce1a4fac0c/resourcegroups/cdw-analysisservices-20200401/providers/microsoft.analysisservices/servers/cdwanalysisservices20200401/providers/Microsoft.ResourceHealth/availabilityStatuses/current",
    "name": "current",
    "type": "Microsoft.ResourceHealth/AvailabilityStatuses",
    "location": "westus2",
    "properties": {
        "availabilityState": "Unavailable",
        "title": "",
        "summary": "Server is paused. Resume the server to make it available. Server is paused. Resume the server to make it available.",
        "reasonType": "Customer Initiated",
        "rootCauseAttributionTime": "2020-04-20T13:15:55.375Z",
        "occuredTime": "2020-04-20T13:15:55Z",
        "reasonChronicity": "Transient",
        "reportedTime": "2020-04-20T13:17:21.4382042Z",
        "resolutionETA": "2020-04-20T13:35:55Z"
    }
}

*/

    internal class HealthResponse {

        public string id { get; set; }

        public string name { get; set; }

        public string type { get; set; }

        public string location { get; set; }

        public Properties properties { get; set; }
    }

    internal class Properties {

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
