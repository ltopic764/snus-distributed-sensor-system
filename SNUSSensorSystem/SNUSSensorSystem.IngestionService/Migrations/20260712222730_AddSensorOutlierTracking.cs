using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SNUSSensorSystem.IngestionService.Migrations
{
    /// <inheritdoc />
    public partial class AddSensorOutlierTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveOutlierCount",
                table: "Sensors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastOutlierMinute",
                table: "Sensors",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsecutiveOutlierCount",
                table: "Sensors");

            migrationBuilder.DropColumn(
                name: "LastOutlierMinute",
                table: "Sensors");
        }
    }
}
