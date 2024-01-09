using Microsoft.Azure.Cosmos.Spatial;
using Newtonsoft.Json;

namespace FunctionApp;

public class Station
{
    [JsonProperty(nameof(id))]
    public required string id { get; set; }

    [JsonProperty("location")]
    public required Point Location { get; set; }

    [JsonProperty("elevation")]
    public double Elevation { get; set; }

    [JsonProperty("state")]
    public required string State { get; set; }

    [JsonProperty("name")]
    public required string Name { get; set; }

    [JsonProperty("flags")]
    public required string[] Flags { get; set; }

    [JsonProperty("wmoId")]
    public required string WMOId { get; set; }
}