using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiddoCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueActiveDailyReportPerChildAndDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DailyReports_ChildId",
                table: "DailyReports");

            migrationBuilder.CreateIndex(
                name: "IX_DailyReports_ChildId_ReportDate",
                table: "DailyReports",
                columns: new[] { "ChildId", "ReportDate" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DailyReports_ChildId_ReportDate",
                table: "DailyReports");

            migrationBuilder.CreateIndex(
                name: "IX_DailyReports_ChildId",
                table: "DailyReports",
                column: "ChildId");
        }
    }
}
