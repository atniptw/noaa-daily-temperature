using Microsoft.Azure.Cosmos.Spatial;
using Newtonsoft.Json;

namespace FunctionApp;

public class Station
{
    [JsonProperty(PropertyName = "id")]
    public required string id { get; set; }

    [JsonProperty(PropertyName = "location")]
    public required Point location { get; set; }

    [JsonProperty(PropertyName = "elevation")]
    public double elevation { get; set; }

    [JsonProperty(PropertyName = "state")]
    public required string state { get; set; }

    [JsonProperty(PropertyName = "name")]
    public required string name { get; set; }

    [JsonProperty(PropertyName = "flags")]
    public required string[] flags { get; set; }

    [JsonProperty(PropertyName = "wmoId")]
    public required string wmoId { get; set; }
}