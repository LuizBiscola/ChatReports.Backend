using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebChat.Migrations
{
    /// <inheritdoc />
    public partial class UpdateChatParticipantAndUserModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatParticipants",
                table: "ChatParticipants");

            migrationBuilder.AddColumn<bool>(
                name: "IsOnline",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeen",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "ChatParticipants",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);            migrationBuilder.Sql("ALTER TABLE \"ChatInsights\" ALTER COLUMN \"KeywordsExtracted\" TYPE jsonb USING \"KeywordsExtracted\"::jsonb");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatParticipants",
                table: "ChatParticipants",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ChatParticipants_ChatId_UserId",
                table: "ChatParticipants",
                columns: new[] { "ChatId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatParticipants",
                table: "ChatParticipants");

            migrationBuilder.DropIndex(
                name: "IX_ChatParticipants_ChatId_UserId",
                table: "ChatParticipants");

            migrationBuilder.DropColumn(
                name: "IsOnline",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastSeen",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ChatParticipants");            migrationBuilder.Sql("ALTER TABLE \"ChatInsights\" ALTER COLUMN \"KeywordsExtracted\" TYPE text");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatParticipants",
                table: "ChatParticipants",
                columns: new[] { "ChatId", "UserId" });
        }
    }
}
