using System.Globalization;
using FunctionApp;

namespace FunctionAppTest;

[TestClass]
public class StationParserTest
{
    [DataRow("AE000041196  25.3330   55.5170   34.0    SHARJAH INTER. AIRP            GSN     41196", "AE000041196", "25.3330", "55.5170", "34.0", "", "SHARJAH INTER. AIRP", "GSN", "41196")]
    [DataRow("USW00093805  30.3975  -84.3289   53.9 FL TALLAHASSEE                        HCN 72214", "USW00093805", "30.3975", "-84.3289", "53.9", "FL", "TALLAHASSEE", "HCN", "72214")]
    [DataTestMethod]
    public void ShouldParseRecord(string input, string id, string lat, string lon, string elevation, string state, string name, string flags, string wmoId)
    {
        var expected = new Station(id, double.Parse(lat, CultureInfo.InvariantCulture), double.Parse(lon, CultureInfo.InvariantCulture), double.Parse(elevation, CultureInfo.InvariantCulture), state, name, flags.Split(','), wmoId);
        var result = StationParser.ParseRecord(input);

        CollectionAssert.AreNotEquivalent(expected.flags, result.flags);
        Assert.AreEqual(expected, result);
    }
}