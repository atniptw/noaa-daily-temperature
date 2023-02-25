using CsvHelper.Configuration;

namespace etl;

public class DailyStationRecordMap : ClassMap<DailyStationRecord>
{
    public DailyStationRecordMap()
    {
        Map(m => m.stationId).Index(0);
        Map(m => m.date).Index(1).TypeConverter<CsvHelper.TypeConversion.DateOnlyConverter>().TypeConverterOption.Format("yyyyMMdd");
        Map(m => m.recordType).Index(2);
        Map(m => m.recordValue).Index(3);
        Map(m => m.measurementFlag).Index(4);
        Map(m => m.qualityFlag).Index(5);
        Map(m => m.sourceFlag).Index(6);
        Map(m => m.observationTime).Index(7);
    }
}
