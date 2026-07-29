using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiddoCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMoodToDailyReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Mood",
                table: "DailyReports",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Mood",
                table: "DailyReports");
        }
    }
}
