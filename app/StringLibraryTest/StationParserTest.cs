using System.Globalization;
using etl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UtilityLibraries;

namespace StringLibraryTest;

[TestClass]
public class StationParserTest
{
    [DataRow("AE000041196  25.3330   55.5170   34.0    SHARJAH INTER. AIRP            GSN     41196", "AE000041196", "25.3330", "55.5170", "34.0", "SHARJAH INTER. AIRP", false)]
    [DataRow("ACW00011604  17.1167  -61.7833   10.1    ST JOHNS COOLIDGE FLD                       ", "ACW00011604", "17.1167", "-61.7833", "10.1", "ST JOHNS COOLIDGE FLD", true)]
    [DataRow("AR000087692 -37.9330  -57.5830   22.0    MAR DEL PLATA AERO             GSN     87692", "AR000087692", "-37.9330", "-57.5830", "22.0", "MAR DEL PLATA AERO", false)]
    [DataRow("ASN00006105 -25.8925  113.5772   33.8    SHARK BAY AIRPORT                      95402", "ASN00006105", "-25.8925", "113.5772", "33.8", "SHARK BAY AIRPORT", false)]
    [DataRow("US1COWE0394  40.6124 -103.9590 1500.2 CO NEW RAYMER 9.4 W                            ", "US1COWE0394", "40.6124", "-103.9590", "1500.2", "NEW RAYMER 9.4 W", true)]
    [DataRow("VE000080425   9.8170  -70.9330   28.0    MENE GRANDE                    GSN     80425", "VE000080425", "9.8170", "-70.9330", "28.0", "MENE GRANDE", false)]
    [DataRow("USW00093805  30.3975  -84.3289   53.9 FL TALLAHASSEE                        HCN 72214", "USW00093805", "30.3975", "-84.3289", "53.9", "TALLAHASSEE", false)]
    [DataRow("USW00093784  39.2814  -76.6111    6.1 MD MARYLAND SCI CTR                   HCN      ", "USW00093784", "39.2814", "-76.6111", "6.1", "MARYLAND SCI CTR", false)]
    [DataRow("ZZZZZZZZZZZ -40.6124 -103.9591 1500.2 YY ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ GSN HCN 00000", "ZZZZZZZZZZZ", "-40.6124", "-103.9591", "1500.2", "ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ", false)]
    [DataTestMethod]
    public void ShouldParseRecord(string input, string id, string lat, string lon, string elevation, string name, bool isSus)
    {
        var expectedStationInfo = new StationInfo(double.Parse(lat, CultureInfo.InvariantCulture), double.Parse(lon, CultureInfo.InvariantCulture), double.Parse(elevation, CultureInfo.InvariantCulture), name, isSus);

        var parser = new StationParser();
        var result = parser.ParseRecord(input);

        Assert.AreEqual(id, result.Key);
        Assert.AreEqual(expectedStationInfo, result.Value);
    }

    [TestMethod]
    public void ShouldParseStationList()
    {
        var stationList = @"
VE000080453   7.3000  -61.4500  181.0    TUMEREMO                       GSN     80453
VE000080462   4.6000  -61.1170  907.0    SANTA ELENA DE UAIR            GSN     80462
VEM00080403  11.4150  -69.6810   15.8    JOSE LEONARDO CHIRINOS                 80403
VEM00080407  10.5580  -71.7280   71.6    LA CHINITA INTL                        80407".Trim();

        var parser = new StationParser();
        var result = parser.ParseStationList(stationList);

        Assert.AreEqual(4, result.Count);
        Assert.AreEqual(new StationInfo(10.5580, -71.7280, 71.6, "LA CHINITA INTL", false), result["VEM00080407"]);
    }
}