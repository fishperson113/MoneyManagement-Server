using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class addTypeToTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Transactions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "expense");

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_Type",
                table: "Transactions",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transaction_Type",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Transactions");
        }
    }
}
