using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingSport.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingRuleSettingsAndDynamicBookingSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_CourtScheduleId_BookingDate",
                table: "Bookings");

            migrationBuilder.AddColumn<Guid>(
                name: "CourtId",
                table: "Bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "EndTime",
                table: "Bookings",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "StartTime",
                table: "Bookings",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BookingRuleSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OpenTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    CloseTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    SlotMinutes = table.Column<int>(type: "integer", nullable: false),
                    DefaultPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingRuleSettings", x => x.Id);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO "BookingRuleSettings"
                    ("Id", "OpenTime", "CloseTime", "SlotMinutes", "DefaultPrice", "IsEnabled", "CreatedAt")
                VALUES
                    ('11111111-1111-1111-1111-111111111111', TIME '05:00', TIME '23:00', 90, 250000, TRUE, NOW())
                ON CONFLICT ("Id") DO NOTHING;
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Bookings" AS b
                SET
                    "CourtId" = cs."CourtId",
                    "StartTime" = cs."StartTime",
                    "EndTime" = cs."EndTime"
                FROM "CourtSchedules" AS cs
                WHERE b."CourtScheduleId" = cs."Id";
                """);

            migrationBuilder.Sql(
                """
                WITH ranked_bookings AS (
                    SELECT
                        b."Id",
                        ROW_NUMBER() OVER (
                            PARTITION BY b."CourtId", b."BookingDate", b."StartTime", b."EndTime"
                            ORDER BY b."CreatedAt", b."Id"
                        ) AS row_number
                    FROM "Bookings" AS b
                    WHERE b."DeletedAt" IS NULL
                      AND b."Status" IN ('Pending', 'Confirmed')
                )
                UPDATE "Bookings" AS b
                SET
                    "Status" = 'Cancelled',
                    "UpdatedAt" = NOW(),
                    "Note" = CASE
                        WHEN COALESCE(b."Note", '') = '' THEN 'Automatically cancelled during migration because a duplicate active booking existed for the same court and time slot.'
                        ELSE b."Note" || ' | Automatically cancelled during migration because a duplicate active booking existed for the same court and time slot.'
                    END
                FROM ranked_bookings AS rb
                WHERE b."Id" = rb."Id" AND rb.row_number > 1;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "CourtId",
                table: "Bookings",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "EndTime",
                table: "Bookings",
                type: "time without time zone",
                nullable: false,
                oldClrType: typeof(TimeOnly),
                oldType: "time without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "StartTime",
                table: "Bookings",
                type: "time without time zone",
                nullable: false,
                oldClrType: typeof(TimeOnly),
                oldType: "time without time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CourtId_BookingDate",
                table: "Bookings",
                columns: new[] { "CourtId", "BookingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CourtId_BookingDate_StartTime_EndTime",
                table: "Bookings",
                columns: new[] { "CourtId", "BookingDate", "StartTime", "EndTime" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL AND \"Status\" IN ('Pending', 'Confirmed')");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CourtScheduleId",
                table: "Bookings",
                column: "CourtScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Courts_CourtId",
                table: "Bookings",
                column: "CourtId",
                principalTable: "Courts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Courts_CourtId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "BookingRuleSettings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CourtId_BookingDate",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CourtId_BookingDate_StartTime_EndTime",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CourtScheduleId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CourtId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CourtScheduleId_BookingDate",
                table: "Bookings",
                columns: new[] { "CourtScheduleId", "BookingDate" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL AND \"Status\" IN ('Pending', 'Confirmed')");
        }
    }
}
