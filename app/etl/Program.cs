// See https://aka.ms/new-console-template for more information
using etl;

var stationFileName = Path.Combine(Directory.GetCurrentDirectory(), "../../data/ghcnd-stations.txt");
var rawStationList = File.ReadAllText(stationFileName);

var parser = new StationParser();
var stations = parser.ParseStationList(rawStationList);

Console.WriteLine($"Loaded {stations.Count} stations");

var weatherDataFileName = Path.Combine(Directory.GetCurrentDirectory(), "../../data/2023.csv");
using var reader = new DailyWeatherReader(weatherDataFileName);
var records = reader.ReadWeatherData();


// Merge with stations
// Output CSV file
