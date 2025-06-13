using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EnrichedLitter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginalLitterId = table.Column<int>(type: "int", nullable: false),
                    Confidence = table.Column<float>(type: "real", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LocationLat = table.Column<float>(type: "real", nullable: true),
                    LocationLon = table.Column<float>(type: "real", nullable: true),
                    WeatherCondition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Temperature = table.Column<float>(type: "real", nullable: true),
                    IsHoliday = table.Column<int>(type: "int", nullable: true),
                    DayOfWeek = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TimeOfDayCategory = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnrichedLitter", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Litter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Confidence = table.Column<float>(type: "real", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LocationLat = table.Column<float>(type: "real", nullable: true),
                    LocationLon = table.Column<float>(type: "real", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Litter", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnrichedLitter");

            migrationBuilder.DropTable(
                name: "Litter");
        }
    }
}
