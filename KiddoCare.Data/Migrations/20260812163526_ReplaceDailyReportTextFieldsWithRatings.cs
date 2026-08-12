using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiddoCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceDailyReportTextFieldsWithRatings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActivityRating",
                table: "DailyReports",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<int>(
                name: "MealRating",
                table: "DailyReports",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<int>(
                name: "SleepRating",
                table: "DailyReports",
                type: "int",
                nullable: false,
                defaultValue: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActivityRating",
                table: "DailyReports");

            migrationBuilder.DropColumn(
                name: "MealRating",
                table: "DailyReports");

            migrationBuilder.DropColumn(
                name: "SleepRating",
                table: "DailyReports");
        }
    }
}
