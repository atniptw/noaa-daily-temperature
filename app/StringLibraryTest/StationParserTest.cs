using etl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UtilityLibraries;

namespace StringLibraryTest;

[TestClass]
public class StationParserTest
{
    [DataRow("AE000041196  25.3330   55.5170   34.0    SHARJAH INTER. AIRP            GSN     41196", "AE000041196", "25.3330", "55.5170")]
    [DataRow("ACW00011604  17.1167  -61.7833   10.1    ST JOHNS COOLIDGE FLD                       ", "ACW00011604", "17.1167", "-61.7833")]
    [DataRow("AR000087692 -37.9330  -57.5830   22.0    MAR DEL PLATA AERO             GSN     87692", "AR000087692", "-37.9330", "-57.5830")]
    [DataRow("ASN00006105 -25.8925  113.5772   33.8    SHARK BAY AIRPORT                      95402", "ASN00006105", "-25.8925", "113.5772")]
    [DataRow("US1COWE0394  40.6124 -103.9590 1500.2 CO NEW RAYMER 9.4 W                            ", "US1COWE0394", "40.6124", "-103.9590")]
    [DataRow("VE000080425   9.8170  -70.9330   28.0    MENE GRANDE                    GSN     80425", "VE000080425", "9.8170",  "-70.9330")]
    [DataTestMethod]
    public void ShouldParseRecord(string input, string name, string lat, string lon)
    {
        var expectedCoordinate = new Coordinate(decimal.Parse(lat), decimal.Parse(lon));

        var parser = new StationParser();
        var result = parser.ParseRecord(input);

        Assert.AreEqual(name, result.Key);
        Assert.AreEqual(expectedCoordinate, result.Value);
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
        Assert.AreEqual(new Coordinate(10.5580m, -71.7280m), result["VEM00080407"]);
    }
}