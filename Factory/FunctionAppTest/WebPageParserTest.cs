using FunctionApp;
using HtmlAgilityPack;

namespace FunctionAppTest;

[TestClass]
public class WebPageParserTest
{
    private static readonly string _html = @"<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 3.2 Final//EN"">
<html>
 <head>
  <title>Index of /pub/data/ghcn/daily/by_year</title>
 </head>
 <body>
<h1>Index of /pub/data/ghcn/daily/by_year</h1>
  <table>
   <tr><th><a href=""?C=N;O=D"">Name</a></th><th><a href=""?C=M;O=A"">Last modified</a></th><th><a href=""?C=S;O=A"">Size</a></th><th><a href=""?C=D;O=A"">Description</a></th></tr>
   <tr><th colspan=""4""><hr></th></tr>
<tr><td><a href=""/pub/data/ghcn/daily/"">Parent Directory</a></td><td>&nbsp;</td><td align=""right"">  - </td><td>&nbsp;</td></tr>
<tr><td><a href=""1750.csv.gz"">1750.csv.gz</a></td><td align=""right"">2022-01-20 14:09  </td><td align=""right""> 64K</td><td>&nbsp;</td></tr>
<tr><td><a href=""1763.csv.gz"">1763.csv.gz</a></td><td align=""right"">2023-11-22 23:30  </td><td align=""right"">3.3K</td><td>&nbsp;</td></tr>
<tr><td><a href=""1764.csv.gz"">1764.csv.gz</a></td><td align=""right"">2023-11-22 23:31  </td><td align=""right"">3.2K</td><td>&nbsp;</td></tr>
<tr><td><a href=""1765.csv.gz"">1765.csv.gz</a></td><td align=""right"">2023-11-22 23:30  </td><td align=""right"">3.3K</td><td>&nbsp;</td></tr>
<tr><td><a href=""1766.csv.gz"">1766.csv.gz</a></td><td align=""right"">2023-11-22 23:32  </td><td align=""right"">3.3K</td><td>&nbsp;</td></tr>
<tr><td><a href=""readme-by_year.txt"">readme-by_year.txt</a></td><td align=""right"">2021-03-08 10:06  </td><td align=""right"">1.1K</td><td>&nbsp;</td></tr>
<tr><td><a href=""status-by_year.txt"">status-by_year.txt</a></td><td align=""right"">2021-04-26 17:54  </td><td align=""right"">169 </td><td>&nbsp;</td></tr>
   <tr><th colspan=""4""><hr></th></tr>
</table>
<script id=""_fed_an_ua_tag"" type=""text/javascript"" src=""https://dap.digitalgov.gov/Universal-Federated-Analytics-Min.js?agency=DOC%26subagency=NOAA""></script></body></html>";

    [TestMethod]
    public void ShouldReturnCorrectNumberOfPairs()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(_html);

        var result = WebPageParser.ParseDocument(doc);

        Assert.AreEqual(5, result.Count);
    }

    [TestMethod]
    public void ShouldReturnCorrectValues()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(_html);

        var result = WebPageParser.ParseDocument(doc);

        var date = new DateTime(2022, 1, 20);
        var expected = new KeyValuePair<string, DateTime>("1750", date);
        Assert.AreEqual(expected, result.First());
    }
}