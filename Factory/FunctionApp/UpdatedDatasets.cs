using System.Net;
using System.Text.Json;
using HtmlAgilityPack;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace FunctionApp;

public class UpdatedDatasets
{
    private const string URL = "https://www.ncei.noaa.gov/pub/data/ghcn/daily/by_year/";
    private readonly ILogger _logger;

    public UpdatedDatasets(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<UpdatedDatasets>();
    }

    [Function("UpdatedDatasets")]
    public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
    {
        var daysAgoTemp = 0;
        if (req.Query["daysAgo"] != null && !int.TryParse(req.Query["daysAgo"], out daysAgoTemp))
        {
            var error = req.CreateResponse(HttpStatusCode.OK);
            error.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            error.WriteString("daysAgo must be a number");

            return error;
        }

        var limit = 0;
        if (req.Query["limit"] != null && !int.TryParse(req.Query["limit"], out limit))
        {
            var error = req.CreateResponse(HttpStatusCode.OK);
            error.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            error.WriteString("limit must be a number");

            return error;
        }

        _logger.LogInformation("Download Page");
        var web = new HtmlWeb();
        var doc = web.Load(URL);

        _logger.LogInformation("Parse Document");
        var results = WebPageParser.ParseDocument(doc);

        _logger.LogInformation("Filter Results");
        var daysAgo = Math.Abs(daysAgoTemp) * (-1);

        var filteredResults = results;
        if (daysAgoTemp > 0)
        {
            filteredResults = results.FindAll(r => r.Value > DateTime.Now.AddDays(daysAgo));
        }

        if (limit > 0)
        {
            filteredResults = filteredResults.GetRange(0, limit);
        }

        var json = JsonSerializer.Serialize(filteredResults.Select(r => r.Key));

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        response.WriteString(json);

        return response;
    }
}
