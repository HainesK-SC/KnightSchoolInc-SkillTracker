using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone.Identity.API.Migrations
{
    /// <inheritdoc />
    public partial class SkillsUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParticipantSkillProgress_Skill_SkillId",
                table: "ParticipantSkillProgress");

            migrationBuilder.DropForeignKey(
                name: "FK_ParticipantSkillProgress_UserProfiles_UserProfileId",
                table: "ParticipantSkillProgress");

            migrationBuilder.DropForeignKey(
                name: "FK_Skill_Discipline_DisciplineId",
                table: "Skill");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Skill",
                table: "Skill");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ParticipantSkillProgress",
                table: "ParticipantSkillProgress");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Discipline",
                table: "Discipline");

            migrationBuilder.RenameTable(
                name: "Skill",
                newName: "Skills");

            migrationBuilder.RenameTable(
                name: "ParticipantSkillProgress",
                newName: "ParticipantSkillProgresses");

            migrationBuilder.RenameTable(
                name: "Discipline",
                newName: "Disciplines");

            migrationBuilder.RenameIndex(
                name: "IX_Skill_DisciplineId",
                table: "Skills",
                newName: "IX_Skills_DisciplineId");

            migrationBuilder.RenameIndex(
                name: "IX_ParticipantSkillProgress_UserProfileId_SkillId",
                table: "ParticipantSkillProgresses",
                newName: "IX_ParticipantSkillProgresses_UserProfileId_SkillId");

            migrationBuilder.RenameIndex(
                name: "IX_ParticipantSkillProgress_SkillId",
                table: "ParticipantSkillProgresses",
                newName: "IX_ParticipantSkillProgresses_SkillId");

            migrationBuilder.RenameIndex(
                name: "IX_Discipline_Name",
                table: "Disciplines",
                newName: "IX_Disciplines_Name");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Skills",
                table: "Skills",
                column: "SkillId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ParticipantSkillProgresses",
                table: "ParticipantSkillProgresses",
                column: "ParticipantSkillProgressId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Disciplines",
                table: "Disciplines",
                column: "DisciplineId");

            migrationBuilder.CreateIndex(
                name: "IX_Skills_Name_DisciplineId",
                table: "Skills",
                columns: new[] { "Name", "DisciplineId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ParticipantSkillProgresses_Skills_SkillId",
                table: "ParticipantSkillProgresses",
                column: "SkillId",
                principalTable: "Skills",
                principalColumn: "SkillId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ParticipantSkillProgresses_UserProfiles_UserProfileId",
                table: "ParticipantSkillProgresses",
                column: "UserProfileId",
                principalTable: "UserProfiles",
                principalColumn: "UserProfileId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Skills_Disciplines_DisciplineId",
                table: "Skills",
                column: "DisciplineId",
                principalTable: "Disciplines",
                principalColumn: "DisciplineId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParticipantSkillProgresses_Skills_SkillId",
                table: "ParticipantSkillProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_ParticipantSkillProgresses_UserProfiles_UserProfileId",
                table: "ParticipantSkillProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_Skills_Disciplines_DisciplineId",
                table: "Skills");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Skills",
                table: "Skills");

            migrationBuilder.DropIndex(
                name: "IX_Skills_Name_DisciplineId",
                table: "Skills");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ParticipantSkillProgresses",
                table: "ParticipantSkillProgresses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Disciplines",
                table: "Disciplines");

            migrationBuilder.RenameTable(
                name: "Skills",
                newName: "Skill");

            migrationBuilder.RenameTable(
                name: "ParticipantSkillProgresses",
                newName: "ParticipantSkillProgress");

            migrationBuilder.RenameTable(
                name: "Disciplines",
                newName: "Discipline");

            migrationBuilder.RenameIndex(
                name: "IX_Skills_DisciplineId",
                table: "Skill",
                newName: "IX_Skill_DisciplineId");

            migrationBuilder.RenameIndex(
                name: "IX_ParticipantSkillProgresses_UserProfileId_SkillId",
                table: "ParticipantSkillProgress",
                newName: "IX_ParticipantSkillProgress_UserProfileId_SkillId");

            migrationBuilder.RenameIndex(
                name: "IX_ParticipantSkillProgresses_SkillId",
                table: "ParticipantSkillProgress",
                newName: "IX_ParticipantSkillProgress_SkillId");

            migrationBuilder.RenameIndex(
                name: "IX_Disciplines_Name",
                table: "Discipline",
                newName: "IX_Discipline_Name");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Skill",
                table: "Skill",
                column: "SkillId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ParticipantSkillProgress",
                table: "ParticipantSkillProgress",
                column: "ParticipantSkillProgressId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Discipline",
                table: "Discipline",
                column: "DisciplineId");

            migrationBuilder.AddForeignKey(
                name: "FK_ParticipantSkillProgress_Skill_SkillId",
                table: "ParticipantSkillProgress",
                column: "SkillId",
                principalTable: "Skill",
                principalColumn: "SkillId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ParticipantSkillProgress_UserProfiles_UserProfileId",
                table: "ParticipantSkillProgress",
                column: "UserProfileId",
                principalTable: "UserProfiles",
                principalColumn: "UserProfileId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Skill_Discipline_DisciplineId",
                table: "Skill",
                column: "DisciplineId",
                principalTable: "Discipline",
                principalColumn: "DisciplineId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
