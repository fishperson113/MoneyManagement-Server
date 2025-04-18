using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class addFKUserIDForWallet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Wallets_AspNetUsers_ApplicationUserId",
                table: "Wallets");

            migrationBuilder.RenameColumn(
                name: "ApplicationUserId",
                table: "Wallets",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Wallets_ApplicationUserId",
                table: "Wallets",
                newName: "IX_Wallet_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Wallets_AspNetUsers_UserId",
                table: "Wallets",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Wallets_AspNetUsers_UserId",
                table: "Wallets");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Wallets",
                newName: "ApplicationUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Wallet_UserId",
                table: "Wallets",
                newName: "IX_Wallets_ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Wallets_AspNetUsers_ApplicationUserId",
                table: "Wallets",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
