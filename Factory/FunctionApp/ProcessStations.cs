using System.Net;
using System.Net.Security;
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
            var stations = new List<Station>();

            using (var httpClientHandler = new HttpClientHandler())
            {
                // httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) =>
                // {
                //     if (sslPolicyErrors == SslPolicyErrors.None)
                //     {
                //         return true;
                //     }

                //     if (cert.GetCertHashString() == "99D5C9E3C60D2AE88006C81E58ECBB68C85A2FFE")
                //     {
                //         return true;
                //     }
                //     return false;
                // };

                using var httpClient = new HttpClient(httpClientHandler);
                var httpResponse = httpClient.GetAsync(URL).Result;
                var content = httpResponse.Content.ReadAsStringAsync().Result;

                using StringReader reader = new StringReader(content);
                _logger.LogInformation("Parse Document");
                string record;
                while ((record = reader.ReadLine()) != null)
                {
                    stations.Add(StationParser.ParseRecord(record));
                }
            }

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
