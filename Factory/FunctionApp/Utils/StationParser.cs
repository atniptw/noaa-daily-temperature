using System.Globalization;
using Microsoft.Azure.Cosmos.Spatial;

namespace FunctionApp;

public class StationParser
{
    public static Station ParseRecord(string record)
    {
        var paddedRecord = record.PadRight(85);

        var id = paddedRecord[0..11];
        var lat = double.Parse(paddedRecord[12..20].Trim(), CultureInfo.InvariantCulture);
        var lon = double.Parse(paddedRecord[21..30].Trim(), CultureInfo.InvariantCulture);
        var elevation = double.Parse(paddedRecord[31..37].Trim(), CultureInfo.InvariantCulture);
        var state = paddedRecord[38..40].Trim();
        var name = paddedRecord[41..71].Trim();
        var gsn = paddedRecord[72..75].Trim();
        var hcnORcrn = paddedRecord[76..79].Trim();
        var wmoId = paddedRecord[80..].Trim();

        var flags = new List<string>();
        if (gsn.Length > 0)
        {
            flags.Add(gsn);
        }

        if (hcnORcrn.Length > 0)
        {
            flags.Add(hcnORcrn);
        }

        return new Station
        {
            id = id,
            Location = new Point(lon, lat),
            Elevation = elevation,
            State = state,
            Name = name,
            Flags = [.. flags],
            WMOId = wmoId
        };
    }
}