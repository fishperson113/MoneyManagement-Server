using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupTransaction_Categories_UserCategoryID",
                table: "GroupTransaction");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupTransaction_GroupFunds_GroupFundID",
                table: "GroupTransaction");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupTransaction_Wallets_UserWalletID",
                table: "GroupTransaction");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GroupTransaction",
                table: "GroupTransaction");

            migrationBuilder.RenameTable(
                name: "GroupTransaction",
                newName: "GroupTransactions");

            migrationBuilder.RenameIndex(
                name: "IX_GroupTransaction_UserWalletID",
                table: "GroupTransactions",
                newName: "IX_GroupTransactions_UserWalletID");

            migrationBuilder.RenameIndex(
                name: "IX_GroupTransaction_UserCategoryID",
                table: "GroupTransactions",
                newName: "IX_GroupTransactions_UserCategoryID");

            migrationBuilder.RenameIndex(
                name: "IX_GroupTransaction_GroupFundID",
                table: "GroupTransactions",
                newName: "IX_GroupTransactions_GroupFundID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GroupTransactions",
                table: "GroupTransactions",
                column: "GroupTransactionID");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupTransactions_Categories_UserCategoryID",
                table: "GroupTransactions",
                column: "UserCategoryID",
                principalTable: "Categories",
                principalColumn: "CategoryID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupTransactions_GroupFunds_GroupFundID",
                table: "GroupTransactions",
                column: "GroupFundID",
                principalTable: "GroupFunds",
                principalColumn: "GroupFundID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupTransactions_Wallets_UserWalletID",
                table: "GroupTransactions",
                column: "UserWalletID",
                principalTable: "Wallets",
                principalColumn: "WalletID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupTransactions_Categories_UserCategoryID",
                table: "GroupTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupTransactions_GroupFunds_GroupFundID",
                table: "GroupTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupTransactions_Wallets_UserWalletID",
                table: "GroupTransactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GroupTransactions",
                table: "GroupTransactions");

            migrationBuilder.RenameTable(
                name: "GroupTransactions",
                newName: "GroupTransaction");

            migrationBuilder.RenameIndex(
                name: "IX_GroupTransactions_UserWalletID",
                table: "GroupTransaction",
                newName: "IX_GroupTransaction_UserWalletID");

            migrationBuilder.RenameIndex(
                name: "IX_GroupTransactions_UserCategoryID",
                table: "GroupTransaction",
                newName: "IX_GroupTransaction_UserCategoryID");

            migrationBuilder.RenameIndex(
                name: "IX_GroupTransactions_GroupFundID",
                table: "GroupTransaction",
                newName: "IX_GroupTransaction_GroupFundID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GroupTransaction",
                table: "GroupTransaction",
                column: "GroupTransactionID");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupTransaction_Categories_UserCategoryID",
                table: "GroupTransaction",
                column: "UserCategoryID",
                principalTable: "Categories",
                principalColumn: "CategoryID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupTransaction_GroupFunds_GroupFundID",
                table: "GroupTransaction",
                column: "GroupFundID",
                principalTable: "GroupFunds",
                principalColumn: "GroupFundID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupTransaction_Wallets_UserWalletID",
                table: "GroupTransaction",
                column: "UserWalletID",
                principalTable: "Wallets",
                principalColumn: "WalletID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
