using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiddoCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateConversationParticipants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Conversations_ChildId_ParentUserId_TeacherUserId",
                table: "Conversations");

            migrationBuilder.AlterColumn<string>(
                name: "TeacherUserId",
                table: "Conversations",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "ParentUserId",
                table: "Conversations",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<int>(
                name: "ChildId",
                table: "Conversations",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "AdminUserId",
                table: "Conversations",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Conversations",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_AdminUserId",
                table: "Conversations",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_ChildId",
                table: "Conversations",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_Type_ChildId_ParentUserId_TeacherUserId_AdminUserId",
                table: "Conversations",
                columns: new[] { "Type", "ChildId", "ParentUserId", "TeacherUserId", "AdminUserId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_AspNetUsers_AdminUserId",
                table: "Conversations",
                column: "AdminUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_AspNetUsers_AdminUserId",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_AdminUserId",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_ChildId",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_Type_ChildId_ParentUserId_TeacherUserId_AdminUserId",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "AdminUserId",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Conversations");

            migrationBuilder.AlterColumn<string>(
                name: "TeacherUserId",
                table: "Conversations",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ParentUserId",
                table: "Conversations",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ChildId",
                table: "Conversations",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_ChildId_ParentUserId_TeacherUserId",
                table: "Conversations",
                columns: new[] { "ChildId", "ParentUserId", "TeacherUserId" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
