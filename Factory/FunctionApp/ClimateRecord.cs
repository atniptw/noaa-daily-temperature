using System.Collections.Immutable;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp;

public class ClimateRecord
{
    private readonly ILogger _logger;
    private static Lazy<CosmosClient> lazyClient = new(InitializeCosmosClient);
    private static CosmosClient cosmosClient => lazyClient.Value;

    private static CosmosClient InitializeCosmosClient()
    {
        var uri = "https://devcosmosnoaa01.documents.azure.com:443";
        var authKey = "";

        return new CosmosClient(uri, authKey);
    }

    public ClimateRecord(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ClimateRecord>();
    }

    [Function("ClimateRecord")]
    public async Task RunAsync([CosmosDBTrigger(
        databaseName: "dev-staging",
        containerName: "ghcn",
        Connection = "CosmosDBConnection",
        LeaseContainerName = "leases",
        CreateLeaseContainerIfNotExists = true)] IReadOnlyList<ClimateDocument> input)
    {
        if (input != null && input.Count > 0)
        {
            Container container = cosmosClient.GetContainer("dev", "stations");
            var stationIds = input.Select(r => r.StationId);
            var query = container.GetItemLinqQueryable<Station>().Where(d => stationIds.Contains(d.id));
            var iterator = query.ToFeedIterator();
            var results = await iterator.ReadNextAsync();

            var stations = results.GroupBy(x => x.id).ToDictionary(x => x.Key, x => x.Single());

            _logger.LogInformation("Documents modified: " + input.Count);
            _logger.LogInformation("First document Id: " + input[0].id);
        }
    }
}

public class ClimateDocument
{
    public required string id { get; set; }

    public required string StationId { get; set; }

    public required string Date { get; set; }

    public required string RecordType { get; set; }

    public double RecordValue { get; set; }

    public required string MeasurementFlag { get; set; }

    public required string QualityFlag { get; set; }

    public required string SourceFlag { get; set; }

    public required string ObservationTime { get; set; }
}
