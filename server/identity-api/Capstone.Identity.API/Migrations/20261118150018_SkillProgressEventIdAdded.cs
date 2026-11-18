using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone.Identity.API.Migrations
{
    /// <inheritdoc />
    public partial class SkillProgressEventIdAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EventId",
                table: "ParticipantSkillProgresses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantSkillProgresses_EventId",
                table: "ParticipantSkillProgresses",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_ParticipantSkillProgresses_Events_EventId",
                table: "ParticipantSkillProgresses",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "EventId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParticipantSkillProgresses_Events_EventId",
                table: "ParticipantSkillProgresses");

            migrationBuilder.DropIndex(
                name: "IX_ParticipantSkillProgresses_EventId",
                table: "ParticipantSkillProgresses");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "ParticipantSkillProgresses");
        }
    }
}
