using Microsoft.AspNetCore.Mvc;
using api;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherInfoController : ControllerBase
{
    private readonly ILogger<WeatherInfoController> _logger;

    public WeatherInfoController(ILogger<WeatherInfoController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Retrieve the historical weather information for the specified date and location
    /// </summary>
    /// <param name="date">Must be in the format: YYYY-MM-DD</param>
    /// <param name="latitude">Latitude</param>
    /// <param name="longitude">Longitude</param>
    /// <returns>Matching weather records within ~30mi</returns>
    [HttpGet(Name = "GetHistoricalWeather")]
    public IEnumerable<WeatherInfo> Get(string date, double latitude, double longitude)
    {
        using var db = new WeatherContext();
        var dataPoints = db.StationData.FromSql($"select *, GreatCircleLength(MakeLine(Location, MakePoint({longitude}, {latitude}, 4326))) as DistanceFromTarget from StationData where RecordDate={date} and PtDistWithin(MakePoint({longitude}, {latitude}, 4326), Location, 48000) order by ST_Distance(MakePoint({longitude}, {latitude}, 4326), Location) ASC").ToList();
        return dataPoints
            .Select(x => new WeatherInfo { Date = x.RecordDate, StationName = x.StationName, IsSus = x.SusStation, HighTemperatureC = x.MaxTemperature, LowTemperatureC = x.MinTemperature, DistanceFromTarget = x.DistanceFromTarget });
    }
}