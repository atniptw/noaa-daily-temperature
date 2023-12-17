using HtmlAgilityPack;

namespace FunctionApp;

public class WebPageParser
{
    public static List<KeyValuePair<string, DateTime>> ParseDocument(HtmlDocument document)
    {
        var table = document.DocumentNode.SelectSingleNode("//table");
        var rows = table.Descendants("tr").Take(table.Descendants("tr").Count() - 3);

        var results = new List<KeyValuePair<string, DateTime>>();
        foreach (var row in rows.Skip(3))
        {
            var cells = row.SelectNodes("td");
            var file = cells[0].InnerText.Trim().Split(".").First();
            var year = DateTime.Parse(cells[1].InnerText.Trim().Split(" ").First());

            var item = new KeyValuePair<string, DateTime>(file, year);

            results.Add(item);
        }

        return results;
    }
}