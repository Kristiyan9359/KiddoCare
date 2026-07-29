using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiddoCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddParentProfileSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ParentProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ParentProfiles");
        }
    }
}
