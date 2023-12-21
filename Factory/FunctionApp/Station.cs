namespace FunctionApp;

public readonly record struct Station(string id, double lat, double lon, double elevation, string state, string name, string[] flags, string wmoId);