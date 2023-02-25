namespace etl;

using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;

public class WeatherContext : DbContext
{
    // public DbSet<Blog> Blogs { get; set; }
    // public DbSet<Post> Posts { get; set; }
    public DbSet<StationDataPoint> StationData { get; set; }

    public string DbPath { get; }

    public WeatherContext()
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        DbPath = System.IO.Path.Join(path, "weather.db");
    }

    // The following configures EF to create a Sqlite database file in the
    // special "local" folder for your platform.
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}", x => x.UseNetTopologySuite());

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StationDataPoint>().Property(c => c.Location).HasSrid(4326);
    }
}

[Index(nameof(StationId), nameof(RecordDate), IsUnique = true)]
public class StationDataPoint
{
    public int Id { get; set; }
    public string StationId { get; set; }
    public DateOnly RecordDate { get; set; }
    public decimal? MaxTemperature { get; set; }
    public decimal? MinTemperature { get; set; }
    public Point Location { get; set; }
}
