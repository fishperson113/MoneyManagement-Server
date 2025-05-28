using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class CreateGroupFundTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GroupFundID",
                table: "Transactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GroupFunds",
                columns: table => new
                {
                    GroupFundID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalFundsIn = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalFundsOut = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupFunds", x => x.GroupFundID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_GroupFundID",
                table: "Transactions",
                column: "GroupFundID");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_GroupFunds_GroupFundID",
                table: "Transactions",
                column: "GroupFundID",
                principalTable: "GroupFunds",
                principalColumn: "GroupFundID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_GroupFunds_GroupFundID",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "GroupFunds");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_GroupFundID",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "GroupFundID",
                table: "Transactions");
        }
    }
}
