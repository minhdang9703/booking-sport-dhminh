using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingSport.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingOverlapExclusionConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE EXTENSION IF NOT EXISTS btree_gist;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Bookings"
                ADD CONSTRAINT "EX_Bookings_NoOverlappingBlockingBookings"
                EXCLUDE USING gist (
                    "CourtId" WITH =,
                    "BookingDate" WITH =,
                    tsrange(
                        "BookingDate"::timestamp + "StartTime",
                        "BookingDate"::timestamp + "EndTime",
                        '[)'
                    ) WITH &&
                )
                WHERE ("DeletedAt" IS NULL AND "Status" IN ('Pending', 'Confirmed'));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Bookings"
                DROP CONSTRAINT IF EXISTS "EX_Bookings_NoOverlappingBlockingBookings";
                """);
        }
    }
}
