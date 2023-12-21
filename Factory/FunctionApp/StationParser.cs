using System.Globalization;

namespace FunctionApp;

public class StationParser
{
    public static Station ParseRecord(string record)
    {
        var id = record[0..11];
        var lat = double.Parse(record[12..20].Trim(), CultureInfo.InvariantCulture);
        var lon = double.Parse(record[21..31].Trim(), CultureInfo.InvariantCulture);
        var elevation = double.Parse(record[31..37].Trim(), CultureInfo.InvariantCulture);
        var state = record[38..40].Trim();
        var name = record[41..71].Trim();
        var gsn = record[73..75].Trim();
        var hcnORcrn = record[77..79].Trim();
        var wmoId = record[80..].Trim();

        var flags = new List<string>();
        if (gsn.Length > 0)
        {
            flags.Add(gsn);
        }

        if (hcnORcrn.Length > 0)
        {
            flags.Add(hcnORcrn);
        }

        return new Station(id, lat, lon, elevation, state, name, [], wmoId);
    }
}