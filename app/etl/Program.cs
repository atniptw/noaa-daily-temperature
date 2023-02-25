// See https://aka.ms/new-console-template for more information
using etl;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

var stationFileName = Path.Combine(Directory.GetCurrentDirectory(), "../../data/ghcnd-stations.txt");
var rawStationList = File.ReadAllText(stationFileName);

var parser = new StationParser();
var stations = parser.ParseStationList(rawStationList);

Console.WriteLine($"Loaded {stations.Count} stations");

var weatherDataFileName = Path.Combine(Directory.GetCurrentDirectory(), "../../data/2023.csv");
using var reader = new DailyWeatherReader(weatherDataFileName);
var records = reader.ReadWeatherData();

using var db = new WeatherContext();
db.Database.Migrate();
Console.WriteLine($"Database path: {db.DbPath}.");

foreach (var record in records)
{
    FormattableString query;

    var temperature = decimal.Parse(record.recordValue) / 10m;
    var coord = stations[record.stationId];
    var location = new Point(coord.lon, coord.lat) { SRID = 4326 };

    if (record.recordType.Equals("TMAX", StringComparison.Ordinal))
    {
        query = $@"
        INSERT INTO StationData (StationId, RecordDate, MaxTemperature, Location)
        VALUES ({record.stationId}, {record.date}, {temperature}, {location})
        ON CONFLICT (StationId, RecordDate) DO UPDATE SET MaxTemperature={temperature};";
    }
    else if (record.recordType.Equals("TMIN", StringComparison.Ordinal))
    {
        query = $@"
        INSERT INTO StationData (StationId, RecordDate, MinTemperature, Location)
        VALUES ({record.stationId}, {record.date}, {temperature}, {location})
        ON CONFLICT (StationId, RecordDate) DO UPDATE SET MinTemperature={temperature};";
    }
    else
    {
        continue;
    }

    // db.Database.ExecuteSql(query);
}

Console.WriteLine($"{db.StationData.Count()} records found");
