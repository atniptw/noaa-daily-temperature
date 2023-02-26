// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
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

int count = 0;
Stopwatch timer = new Stopwatch();
timer.Start();
var transaction = db.Database.BeginTransaction();

var currentRecord = records.GetEnumerator();
var enumeratorStatus = currentRecord.MoveNext();

while (enumeratorStatus)
{
    FormattableString query;

    var record = currentRecord.Current;
    var temperature = decimal.Parse(record.recordValue) / 10m;
    var station = stations[record.stationId];
    var location = new Point(station.lon, station.lat) { SRID = 4326 };

    var oldRecordType = record.recordType;
    enumeratorStatus = currentRecord.MoveNext();
    var nextRecord = currentRecord.Current;

    // Take advantage of the reality that the data is sorted when we did it, so we can not worry about conflicts
    // and merge data from adjacent lines into a single insert in the common case
    if (enumeratorStatus && nextRecord.stationId == record.stationId && nextRecord.date == record.date && !nextRecord.recordType.Equals(oldRecordType))
    {
        decimal maxTemperature;
        decimal minTemperature;
        // Insert both
        if (record.recordType.Equals("TMAX", StringComparison.Ordinal) && nextRecord.recordType.Equals("TMIN"))
        {
            maxTemperature = temperature;
            minTemperature = decimal.Parse(nextRecord.recordValue) / 10m;
        }
        else if (record.recordType.Equals("TMIN", StringComparison.Ordinal) && nextRecord.recordType.Equals("TMAX"))
        {
            minTemperature = temperature;
            maxTemperature = decimal.Parse(nextRecord.recordValue) / 10m;
        }
        else
        {
            continue;
        }
        query = $@"
        INSERT INTO StationData (StationId, StationName, SusStation, RecordDate, MaxTemperature, MinTemperature, Location)
        VALUES ({record.stationId}, {station.name}, {station.isSus}, {record.date}, {maxTemperature}, {minTemperature}, {location})
        ";
        enumeratorStatus = currentRecord.MoveNext();

    }
    else
    {
        // Insert just one
        if (record.recordType.Equals("TMAX", StringComparison.Ordinal))
        {
            query = $@"
        INSERT INTO StationData (StationId, StationName, SusStation, RecordDate, MaxTemperature, Location)
        VALUES ({record.stationId}, {station.name}, {station.isSus}, {record.date}, {temperature}, {location})
        ";
        }
        else if (record.recordType.Equals("TMIN", StringComparison.Ordinal))
        {
            query = $@"
        INSERT INTO StationData (StationId, StationName, SusStation, RecordDate, MinTemperature, Location)
        VALUES ({record.stationId}, {station.name}, {station.isSus}, {record.date}, {temperature}, {location})
        ";
        }
        else
        {
            continue;
        }
    }

    db.Database.ExecuteSql(query);

    count++;
    int interval = 1000;
    if (count % interval == 0)
    {
        transaction.Commit();
        transaction.Dispose();
        timer.Stop();
        Console.WriteLine($"Upserted {count} records, {timer.ElapsedMilliseconds}ms per {interval}");
        timer.Restart();
        transaction = db.Database.BeginTransaction();
    }
}

Console.WriteLine($"{db.StationData.Count()} records found");
