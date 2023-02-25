using System.Globalization;

namespace etl;

public class StationParser
{
    public KeyValuePair<string, Coordinate> ParseRecord(string record)
    {
        if (record.Length < 32)
        {
            throw new ArgumentException("Invalid record length", nameof(record));
        }

        var name = record[0..11];
        var lat = double.Parse(record[12..20].Trim(), CultureInfo.InvariantCulture);
        var lon = double.Parse(record[21..31].Trim(), CultureInfo.InvariantCulture);

        return new KeyValuePair<string, Coordinate>(name, new Coordinate(lat, lon));
    }

    public Dictionary<string, Coordinate> ParseStationList(string list)
    {
        var result = new Dictionary<string, Coordinate>();

        var lines = list.Split('\n');
        foreach (var line in lines)
        {
            try
            {
                var record = ParseRecord(line);
                result.Add(record.Key, record.Value);
            }
            catch (ArgumentException)
            {
                continue;
            }
        }

        return result;
    }
}