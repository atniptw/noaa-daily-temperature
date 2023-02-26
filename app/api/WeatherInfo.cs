namespace api;

public class WeatherInfo
{
    public DateOnly Date { get; set; }

    public string StationName { get; set; }

    /// <summary>
    /// A station is considered to be SUS if it has no WMO ID and is not a part of any US Climate Network
    /// </summary>
    public bool IsSus { get; set; }

    public decimal? HighTemperatureC { get; set; }

    public decimal? HighTemperatureF => HighTemperatureC.HasValue ? Math.Round(32m + HighTemperatureC.Value / 0.5556m) : null;

    public decimal? LowTemperatureC { get; set; }

    public decimal? LowTemperatureF => LowTemperatureC.HasValue ? Math.Round(32m + LowTemperatureC.Value / 0.5556m) : null;

    /// <summary>
    /// Distance in meters from the requested location
    /// </summary>
    public decimal DistanceFromTarget { get; set; }
}
