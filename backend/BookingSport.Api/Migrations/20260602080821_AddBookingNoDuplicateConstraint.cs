using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingSport.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingNoDuplicateConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_CourtScheduleId_BookingDate",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CourtScheduleId_BookingDate",
                table: "Bookings",
                columns: new[] { "CourtScheduleId", "BookingDate" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL AND \"Status\" IN ('Pending', 'Confirmed')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_CourtScheduleId_BookingDate",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CourtScheduleId_BookingDate",
                table: "Bookings",
                columns: new[] { "CourtScheduleId", "BookingDate" });
        }
    }
}
