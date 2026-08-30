using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NellisScanner.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAuctionCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Auctions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CategoryName",
                table: "Auctions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Auctions_CategoryId",
                table: "Auctions",
                column: "CategoryId");

            // Backfill CategoryId/CategoryName for existing auctions by joining to Inventory
            // (Inventory.CategoryName was stored as the enum name, e.g. "Electronics").
            migrationBuilder.Sql("""
                UPDATE "Auctions" a
                SET "CategoryName" = inv."CategoryName",
                    "CategoryId" = CASE inv."CategoryName"
                        WHEN 'Electronics' THEN 1
                        WHEN 'HomeAndHousehold' THEN 2
                        WHEN 'HomeImprovement' THEN 3
                        WHEN 'SmartHome' THEN 4
                        WHEN 'OfficeAndSchool' THEN 5
                        WHEN 'Automotive' THEN 6
                        ELSE NULL
                    END
                FROM "Inventory" inv
                WHERE a."InventoryNumber" = inv."InventoryNumber"
                  AND a."CategoryId" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Auctions_CategoryId",
                table: "Auctions");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Auctions");

            migrationBuilder.DropColumn(
                name: "CategoryName",
                table: "Auctions");
        }
    }
}
