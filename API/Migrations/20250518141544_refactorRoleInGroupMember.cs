using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class refactorRoleInGroupMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAdmin",
                table: "GroupMembers");

            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "GroupMembers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "GroupMembers");

            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                table: "GroupMembers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
