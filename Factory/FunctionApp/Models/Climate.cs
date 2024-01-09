using Newtonsoft.Json;

namespace FunctionApp;

public class Climate
{
    [JsonProperty("station")]
    public required Station Station { get; set; }

    [JsonProperty("date")]
    public DateOnly Date { get; set; }

    [JsonProperty("recordType")]
    public required string RecordType { get; set; }

    [JsonProperty("recordValue")]
    public required double RecordValue { get; set; }

    [JsonProperty("measurementFlag")]
    public char? MeasurementFlag { get; set; }

    [JsonProperty("qualityFlag")]
    public char? QualityFlag { get; set; }

    [JsonProperty("sourceFlag")]
    public char? SourceFlag { get; set; }

    [JsonProperty("observationTime")]
    public required string ObservationTime { get; set; }
}