using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone.Identity.API.Migrations
{
    /// <inheritdoc />
    public partial class EventEnrollmentAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventEnrollments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    UserProfileId = table.Column<int>(type: "int", nullable: false),
                    EnrolledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EnrolledById = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventEnrollments_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "EventId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventEnrollments_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "UserProfileId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventEnrollments_EventId_UserProfileId",
                table: "EventEnrollments",
                columns: new[] { "EventId", "UserProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventEnrollments_UserProfileId",
                table: "EventEnrollments",
                column: "UserProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventEnrollments");
        }
    }
}
