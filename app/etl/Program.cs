// See https://aka.ms/new-console-template for more information
using System.IO;
using etl;


var stationFileName = Path.Combine(Directory.GetCurrentDirectory(), "../../data/ghcnd-stations.txt");
var rawStationList = File.ReadAllText(stationFileName);

var parser = new StationParser();
var stations = parser.ParseStationList(rawStationList);

Console.WriteLine($"Loaded {stations.Count} stations");
