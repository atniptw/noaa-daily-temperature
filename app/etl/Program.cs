// See https://aka.ms/new-console-template for more information
using etl;

var stationFileName = Path.Combine(Directory.GetCurrentDirectory(), "../../data/ghcnd-stations.txt");
var rawStationList = File.ReadAllText(stationFileName);

var parser = new StationParser();
var stations = parser.ParseStationList(rawStationList);

Console.WriteLine($"Loaded {stations.Count} stations");

var weatherDataFileName = Path.Combine(Directory.GetCurrentDirectory(), "../../data/2023.csv");
var reader = new DailyWeatherReader();
reader.ReadWeatherData(weatherDataFileName, r => Console.WriteLine(r));


// Merge with stations
// Output CSV file
