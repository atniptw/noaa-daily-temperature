namespace FunctionApp;

public record Station(
    string id,
    string name,
    CosmosPoint location,
    double elevation,
    string state,
    string[] flags,
    string wmoId
);