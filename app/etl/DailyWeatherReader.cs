using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace etl;

public class DailyWeatherReader
{
    public void ReadWeatherData(string fileName, Action<DailyStationRecord> action)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
            ShouldSkipRecord = (args) => !args.Row[2].Equals("TMAX") && !args.Row[2].Equals("TMIN")
        };

        using (var reader = new StreamReader(fileName))
        using (var csv = new CsvReader(reader, config))
        {
            csv.Context.RegisterClassMap<DailyStationRecordMap>();
            var records = csv.GetRecords<DailyStationRecord>();

            foreach (var record in records)
            {
                action(record);
            }
        }
    }
}
