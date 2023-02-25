using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace etl;

public class DailyWeatherReader : IDisposable
{
    private bool disposedValue;
    private readonly StreamReader reader;
    private readonly CsvReader csv;

    public DailyWeatherReader(string fileName)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
            ShouldSkipRecord = (args) => !args.Row[2].Equals("TMAX") && !args.Row[2].Equals("TMIN")
        };

        reader = new StreamReader(fileName);
        csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<DailyStationRecordMap>();
    }

    public IEnumerable<DailyStationRecord> ReadWeatherData()
    {
        return csv.GetRecords<DailyStationRecord>();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                csv.Dispose();
                reader.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
