using System.Globalization;
using FunctionApp;
using Microsoft.Azure.Cosmos.Spatial;

namespace FunctionAppTest;

[TestClass]
public class StationParserTest
{
    [DataRow("00000000001  25.3330   55.5170   34.0    SHARJAH INTER. AIRP            GSN     41196", "00000000001", "25.3330", "55.5170", "34.0", "", "SHARJAH INTER. AIRP", "GSN", "41196")]
    [DataRow("00000000002  30.3975  -84.3289   53.9 FL TALLAHASSEE                        HCN 72214", "00000000002", "30.3975", "-84.3289", "53.9", "FL", "TALLAHASSEE", "HCN", "72214")]
    [DataRow("00000000003  30.3975  -84.3289   53.9 FL TALLAHASSEE                    GSN HCN 72214", "00000000003", "30.3975", "-84.3289", "53.9", "FL", "TALLAHASSEE", "GSN,HCN", "72214")]
    [DataRow("00000000004  30.3975  -84.3289   53.9 FL TALLAHASSEE                    GSN CRN 72214", "00000000004", "30.3975", "-84.3289", "53.9", "FL", "TALLAHASSEE", "GSN,CRN", "72214")]
    [DataRow("00000000005  30.3975  -84.3289   53.9 FL TALLAHASSEE                                 ", "00000000005", "30.3975", "-84.3289", "53.9", "FL", "TALLAHASSEE", "", "")]
    [DataRow("00000000005  30.3975  -84.3289   53.9 FL TALLAHASSEE", "00000000005", "30.3975", "-84.3289", "53.9", "FL", "TALLAHASSEE", "", "")]
    [DataRow("00000000006  30.3975  -84.3289   53.9 FL ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ GSN CRN 72214", "00000000006", "30.3975", "-84.3289", "53.9", "FL", "ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ", "GSN,CRN", "72214")]
    [DataRow("00000000007  30.3975  -84.3289   53.9    ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ              ", "00000000007", "30.3975", "-84.3289", "53.9", "", "ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ", "", "")]
    [DataRow("00000000008  25.3330   55.5170   34.0    SHARJAH INTER. AIRP            GSN     41196", "00000000008", "25.3330", "55.5170", "34.0", "", "SHARJAH INTER. AIRP", "GSN", "41196")]
    [DataRow("00000000009  17.1167  -61.7833   10.1    ST JOHNS COOLIDGE FLD                       ", "00000000009", "17.1167", "-61.7833", "10.1", "", "ST JOHNS COOLIDGE FLD", "", "")]
    [DataRow("00000000010 -37.9330  -57.5830   22.0    MAR DEL PLATA AERO             GSN     87692", "00000000010", "-37.9330", "-57.5830", "22.0", "", "MAR DEL PLATA AERO", "GSN", "87692")]
    [DataRow("00000000011 -25.8925  113.5772   33.8    SHARK BAY AIRPORT                      95402", "00000000011", "-25.8925", "113.5772", "33.8", "", "SHARK BAY AIRPORT", "", "95402")]
    [DataRow("00000000012  40.6124 -103.9590 1500.2 CO NEW RAYMER 9.4 W                            ", "00000000012", "40.6124", "-103.9590", "1500.2", "CO", "NEW RAYMER 9.4 W", "", "")]
    [DataRow("00000000012  40.6124 -103.9590 -999.9 CO NEW RAYMER 9.4 W                            ", "00000000012", "40.6124", "-103.9590", "-999.9", "CO", "NEW RAYMER 9.4 W", "", "")]
    [DataRow("00000000012  40.6124 -103.9590 -999.9 CO NEW RAYMER 9.4 W", "00000000012", "40.6124", "-103.9590", "-999.9", "CO", "NEW RAYMER 9.4 W", "", "")]
    [DataTestMethod]
    public void ShouldParseRecord(string input, string id, string lat, string lon, string elevation, string state, string name, string flags, string wmoId)
    {
        var flagsSplit = flags.Split(',');
        var emptyRemoved = flagsSplit.Where(x => !string.IsNullOrEmpty(x)).ToArray();
        var expected = new Station
        {
            id = id,
            location = new Point(double.Parse(lon, CultureInfo.InvariantCulture), double.Parse(lat, CultureInfo.InvariantCulture)),
            elevation = double.Parse(elevation, CultureInfo.InvariantCulture),
            state = state,
            name = name,
            flags = [.. emptyRemoved],
            wmoId = wmoId
        };
        var result = StationParser.ParseRecord(input);

        Assert.AreEqual(expected.id, result.id);
        Assert.AreEqual(expected.location, result.location);
        Assert.AreEqual(expected.elevation, result.elevation);
        Assert.AreEqual(expected.state, result.state);
        Assert.AreEqual(expected.name, result.name);
        CollectionAssert.AreEquivalent(expected.flags, result.flags);
        Assert.AreEqual(expected.wmoId, result.wmoId);
    }
}