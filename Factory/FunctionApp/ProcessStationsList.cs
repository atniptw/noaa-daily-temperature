using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp;

public class ProcessStationsList(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<ProcessStationsList>();

    [Function("ProcessStationsList")]
    [BlobOutput("ghcnd-stations/stations.json", Connection = "Stations")]
    public async Task<string> RunAsync([TimerTrigger("0 0 0 1 * *", RunOnStartup = true)] TimerInfo myTimer)
    {
        _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
        }

        var client = new HttpClient
        {
            BaseAddress = new Uri("https://www.ncei.noaa.gov")
        };
        using var response = await client.GetAsync("/pub/data/ghcn/daily/ghcnd-stations.txt");

        var stations = new List<Station>();
        using (var reader = new StreamReader(await response.Content.ReadAsStreamAsync()))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                stations.Add(StationParser.ParseRecord(line));
            }
        }

        return JsonSerializer.Serialize(stations);
    }
}
