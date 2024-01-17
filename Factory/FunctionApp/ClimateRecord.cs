using System.Collections.Immutable;
using System.Globalization;
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
        var authKey = "UgaghEjsiczVJRyJxrwz3Fwz7hAR3LCUDReUmoVQXs2OHX6MJ3Bk6yhKh4dNrOHkddIygOdakVBnACDblcOn1A==";

        return new CosmosClient(uri, authKey);
    }

    public ClimateRecord(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ClimateRecord>();
    }

    [Function("ClimateRecord")]
    [ExponentialBackoffRetry(5, "00:00:04", "00:15:00")]
    [CosmosDBOutput("dev", "stations", Connection = "CosmosDBConnection", CreateIfNotExists = true)]
    public async Task<List<Climate>?> RunAsync([CosmosDBTrigger(
        databaseName: "dev-staging",
        containerName: "ghcn",
        Connection = "CosmosDBConnection",
        LeaseContainerName = "leases",
        CreateLeaseContainerIfNotExists = true)] IReadOnlyList<ClimateDocument> input)
    {
        if (input != null && input.Count > 0)
        {
            try
            {
                Container container = cosmosClient.GetContainer("dev", "stations");
                // var stationIds = input.Select(r => r.StationId);
                var stationIds = new List<string>
                {
                    "ASN00001006"
                };
                var query = container.GetItemLinqQueryable<Station>().Where(d => stationIds.Contains(d.id));
                var iterator = query.ToFeedIterator();
                var results = await iterator.ReadNextAsync();

                var stations = results.GroupBy(x => x.id).ToDictionary(x => x.Key, x => x.Single());

                _logger.LogInformation("Documents modified: " + input.Count);
                var climateReadings = new List<Climate>();
                for (var i = 0; i < input.Count; i++)
                {
                    var document = input[i];
                    try
                    {
                        var date = DateOnly.ParseExact(document.Date, "yyyyMMdd", CultureInfo.InvariantCulture);
                        var record = new Climate
                        {
                            Id = $"{document.StationId}-{document.Date}",
                            Station = stations[document.StationId],
                            Date = date
                        };

                        var reading = new Reading
                        {
                            RecordValue = document.RecordValue,
                            MeasurementFlag = document.MeasurementFlag,
                            QualityFlag = document.QualityFlag,
                            SourceFlag = document.SourceFlag,
                            ObservationTime = document.ObservationTime
                        };

                        switch (document.RecordType)
                        {
                            case "PRCP":
                                record.Precipitation = reading;
                                break;
                            case "SNOW":
                                record.Snowfall = reading;
                                break;
                            case "SNWD":
                                record.SnowDepth = reading;
                                break;
                            case "TMAX":
                                record.MaxTemperature = reading;
                                break;
                            case "TMIN":
                                record.MinTemperature = reading;
                                break;
                            default:
                                break;
                        }

                        climateReadings.Add(record);
                    }
                    catch (Exception)
                    {
                        _logger.LogError("Unable to parse record: " + document.id);
                    }
                }

                return climateReadings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        return null;
    }
}

public class ClimateDocument
{
    public required string id { get; set; }

    public required string StationId { get; set; }

    public required string Date { get; set; }

    public required string RecordType { get; set; }

    public double RecordValue { get; set; }

    public string? MeasurementFlag { get; set; }

    public string? QualityFlag { get; set; }

    public string? SourceFlag { get; set; }

    public string? ObservationTime { get; set; }
}
