using System.Net;
using System.Text.Json;
using HtmlAgilityPack;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace FunctionApp;

public class ProcessStations(ILoggerFactory loggerFactory)
{
    private const string URL = "https://www.ncei.noaa.gov/pub/data/ghcn/daily/ghcnd-stations.txt";
    private const string FILENAME = "/workspaces/noaa-daily-temperature/Factory/FunctionApp/ghcnd-stations.txt";
    private readonly ILogger _logger = loggerFactory.CreateLogger<ProcessStations>();

    [Function("ProcessStations")]
    public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation("Download Page");
            var web = new HtmlWeb();
            var doc = web.Load(URL);

            var stations = new List<Station>();

            using (StringReader reader = new StringReader(doc.Text))
            {
                _logger.LogInformation("Parse Document");
                string record;
                while ((record = reader.ReadLine()) != null)
                {
                    stations.Add(StationParser.ParseRecord(record));
                }
            }

            // var fileLines = File.ReadAllLines(FILENAME);
            // foreach (var record in fileLines)
            // {
            //     stations.Add(StationParser.ParseRecord(record));
            // }


            var json = JsonSerializer.Serialize(stations);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(json);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());

            var response = req.CreateResponse(HttpStatusCode.ServiceUnavailable);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            return response;
        }
    }
}
