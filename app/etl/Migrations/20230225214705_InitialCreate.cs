using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace etl.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Sqlite:InitSpatialMetaData", true);

            migrationBuilder.CreateTable(
                name: "StationData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StationId = table.Column<string>(type: "TEXT", nullable: false),
                    RecordDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    MaxTemperature = table.Column<decimal>(type: "TEXT", nullable: true),
                    MinTemperature = table.Column<decimal>(type: "TEXT", nullable: true),
                    Location = table.Column<Point>(type: "POINT", nullable: false)
                        .Annotation("Sqlite:Srid", 4326)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StationData", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StationData_StationId_RecordDate",
                table: "StationData",
                columns: new[] { "StationId", "RecordDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StationData");
        }
    }
}
