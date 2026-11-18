using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone.Identity.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedEventStaffAssignment2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EventsStaffAssignments_EventId_UserProfileId",
                table: "EventsStaffAssignments");

            migrationBuilder.DropColumn(
                name: "UserProfileId",
                table: "EventsStaffAssignments");

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAtUtc",
                table: "EventsStaffAssignments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedBy",
                table: "EventsStaffAssignments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "EventsStaffAssignments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_EventsStaffAssignments_EventId_UserId",
                table: "EventsStaffAssignments",
                columns: new[] { "EventId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventsStaffAssignments_UserId",
                table: "EventsStaffAssignments",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_EventsStaffAssignments_AspNetUsers_UserId",
                table: "EventsStaffAssignments",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EventsStaffAssignments_Events_EventId",
                table: "EventsStaffAssignments",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "EventId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventsStaffAssignments_AspNetUsers_UserId",
                table: "EventsStaffAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_EventsStaffAssignments_Events_EventId",
                table: "EventsStaffAssignments");

            migrationBuilder.DropIndex(
                name: "IX_EventsStaffAssignments_EventId_UserId",
                table: "EventsStaffAssignments");

            migrationBuilder.DropIndex(
                name: "IX_EventsStaffAssignments_UserId",
                table: "EventsStaffAssignments");

            migrationBuilder.DropColumn(
                name: "AssignedAtUtc",
                table: "EventsStaffAssignments");

            migrationBuilder.DropColumn(
                name: "AssignedBy",
                table: "EventsStaffAssignments");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "EventsStaffAssignments");

            migrationBuilder.AddColumn<int>(
                name: "UserProfileId",
                table: "EventsStaffAssignments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_EventsStaffAssignments_EventId_UserProfileId",
                table: "EventsStaffAssignments",
                columns: new[] { "EventId", "UserProfileId" },
                unique: true);
        }
    }
}
