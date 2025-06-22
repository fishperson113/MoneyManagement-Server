using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class PostCommentReply : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PostCommentReplies",
                columns: table => new
                {
                    ReplyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CommentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ParentReplyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostCommentReplies", x => x.ReplyId);
                    table.ForeignKey(
                        name: "FK_PostCommentReplies_AspNetUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PostCommentReplies_PostCommentReplies_ParentReplyId",
                        column: x => x.ParentReplyId,
                        principalTable: "PostCommentReplies",
                        principalColumn: "ReplyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PostCommentReplies_PostComments_CommentId",
                        column: x => x.CommentId,
                        principalTable: "PostComments",
                        principalColumn: "CommentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PostCommentReplies_AuthorId",
                table: "PostCommentReplies",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_PostCommentReplies_CommentId",
                table: "PostCommentReplies",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_PostCommentReplies_ParentReplyId",
                table: "PostCommentReplies",
                column: "ParentReplyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PostCommentReplies");
        }
    }
}
