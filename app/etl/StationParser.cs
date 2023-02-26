using System.Globalization;

namespace etl;

public class StationParser
{
    public KeyValuePair<string, StationInfo> ParseRecord(string record)
    {
        if (record.Length < 32)
        {
            throw new ArgumentException("Invalid record length", nameof(record));
        }

        var id = record[0..11];
        var lat = double.Parse(record[12..20].Trim(), CultureInfo.InvariantCulture);
        var lon = double.Parse(record[21..31].Trim(), CultureInfo.InvariantCulture);
        var elevation = double.Parse(record[31..38].Trim(), CultureInfo.InvariantCulture);
        var name = record[41..71].Trim();
        var isSus = record[76..].Trim().Length == 0;

        return new KeyValuePair<string, StationInfo>(id, new StationInfo(lat, lon, elevation, name, isSus));
    }

    public Dictionary<string, StationInfo> ParseStationList(string list)
    {
        var result = new Dictionary<string, StationInfo>();

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