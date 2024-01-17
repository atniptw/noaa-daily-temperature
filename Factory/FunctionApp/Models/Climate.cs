using Newtonsoft.Json;

namespace FunctionApp;

public class Climate
{
    [JsonProperty("id")]
    public required string Id { get; set; }

    [JsonProperty("station")]
    public required Station Station { get; set; }

    [JsonProperty("date")]
    public DateOnly Date { get; set; }

    [JsonProperty("PRCP")]
    public Reading? Precipitation { get; set; }

    [JsonProperty("SNOW")]
    public Reading? Snowfall { get; set; }

    [JsonProperty("SNWD")]
    public Reading? SnowDepth { get; set; }

    [JsonProperty("TMAX")]
    public Reading? MaxTemperature { get; set; }

    [JsonProperty("TMIN")]
    public Reading? MinTemperature { get; set; }
}

public class Reading
{
    [JsonProperty("recordValue")]
    public required double RecordValue { get; set; }

    [JsonProperty("measurementFlag")]
    public string? MeasurementFlag { get; set; }

    [JsonProperty("qualityFlag")]
    public string? QualityFlag { get; set; }

    [JsonProperty("sourceFlag")]
    public string? SourceFlag { get; set; }

    [JsonProperty("observationTime")]
    public string? ObservationTime { get; set; }
}
