using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone.Identity.API.Migrations
{
    /// <inheritdoc />
    public partial class ProgressStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_ParticipantSkillProgresses_Status_NotLocked",
                table: "ParticipantSkillProgresses",
                sql: "[Status] <> 'Locked'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ParticipantSkillProgresses_Status_NotLocked",
                table: "ParticipantSkillProgresses");
        }
    }
}
