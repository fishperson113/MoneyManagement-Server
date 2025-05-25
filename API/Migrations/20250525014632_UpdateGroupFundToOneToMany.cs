using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGroupFundToOneToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_GroupFunds_GroupID",
                table: "GroupFunds",
                column: "GroupID");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupFunds_Groups_GroupID",
                table: "GroupFunds",
                column: "GroupID",
                principalTable: "Groups",
                principalColumn: "GroupId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupFunds_Groups_GroupID",
                table: "GroupFunds");

            migrationBuilder.DropIndex(
                name: "IX_GroupFunds_GroupID",
                table: "GroupFunds");
        }
    }
}
