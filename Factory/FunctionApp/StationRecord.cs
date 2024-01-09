using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp;

public class StationRecord
{
    private readonly ILogger _logger;

    public StationRecord(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<StationRecord>();
    }

    [Function("StationRecord")]
    [ExponentialBackoffRetry(5, "00:00:04", "00:15:00")]
    [CosmosDBOutput("dev", "stations", Connection = "CosmosDBConnection", CreateIfNotExists = true)]
    public List<Station>? Run([CosmosDBTrigger(
        databaseName: "dev-staging",
        containerName: "stations",
        Connection = "CosmosDBConnection",
        LeaseContainerName = "leases",
        CreateLeaseContainerIfNotExists = true,
        MaxItemsPerInvocation = 1000)] IReadOnlyList<StationDocument> input)
    {
        if (input != null && input.Count > 0)
        {
            _logger.LogInformation("Documents modified: " + input.Count);
            var stations = new List<Station>();
            for (var i = 0; i < input.Count; i++)
            {
                var document = input[i];
                try
                {
                    stations.Add(StationParser.ParseRecord(document.record));
                }
                catch (Exception)
                {
                    _logger.LogError("Unable to parse record: " + document.id);
                }
            }
            return stations;
        }

        return null;
    }
}

public class StationDocument
{
    public required string id { get; set; }

    public required string record { get; set; }
}
