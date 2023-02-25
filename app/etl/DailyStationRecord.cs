namespace etl;

public readonly record struct DailyStationRecord(string stationId, DateOnly date, string recordType, string recordValue, char? measurementFlag, char? qualityFlag, char? sourceFlag, string observationTime);