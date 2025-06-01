using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupTransactionFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_GroupFunds_GroupFundID",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_GroupFundID",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "GroupFundID",
                table: "Transactions");

            migrationBuilder.AddColumn<decimal>(
                name: "SavingGoal",
                table: "GroupFunds",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "GroupTransaction",
                columns: table => new
                {
                    GroupTransactionID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupFundID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserWalletID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserCategoryID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<string>(type: "varchar(20)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupTransaction", x => x.GroupTransactionID);
                    table.ForeignKey(
                        name: "FK_GroupTransaction_Categories_UserCategoryID",
                        column: x => x.UserCategoryID,
                        principalTable: "Categories",
                        principalColumn: "CategoryID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupTransaction_GroupFunds_GroupFundID",
                        column: x => x.GroupFundID,
                        principalTable: "GroupFunds",
                        principalColumn: "GroupFundID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupTransaction_Wallets_UserWalletID",
                        column: x => x.UserWalletID,
                        principalTable: "Wallets",
                        principalColumn: "WalletID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroupTransaction_GroupFundID",
                table: "GroupTransaction",
                column: "GroupFundID");

            migrationBuilder.CreateIndex(
                name: "IX_GroupTransaction_UserCategoryID",
                table: "GroupTransaction",
                column: "UserCategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_GroupTransaction_UserWalletID",
                table: "GroupTransaction",
                column: "UserWalletID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupTransaction");

            migrationBuilder.DropColumn(
                name: "SavingGoal",
                table: "GroupFunds");

            migrationBuilder.AddColumn<Guid>(
                name: "GroupFundID",
                table: "Transactions",
                type: "uniqueidentifier",
                nullable: true);

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
    }
}
