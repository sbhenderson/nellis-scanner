using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using NellisScanner.Core.Models;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NellisScanner.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAuctionInventorySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriceHistory");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.CreateTable(
                name: "Inventory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InventoryNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CategoryName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FirstSeen = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeen = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventory", x => x.Id);
                    table.UniqueConstraint("AK_Inventory_InventoryNumber", x => x.InventoryNumber);
                });

            migrationBuilder.CreateTable(
                name: "Auctions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InventoryNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RetailPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CurrentPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    FinalPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    OpenTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CloseTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: true),
                    BidCount = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auctions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Auctions_Inventory_InventoryNumber",
                        column: x => x.InventoryNumber,
                        principalTable: "Inventory",
                        principalColumn: "InventoryNumber",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Auctions_CloseTime",
                table: "Auctions",
                column: "CloseTime");

            migrationBuilder.CreateIndex(
                name: "IX_Auctions_InventoryNumber",
                table: "Auctions",
                column: "InventoryNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_InventoryNumber",
                table: "Inventory",
                column: "InventoryNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Auctions");

            migrationBuilder.DropTable(
                name: "Inventory");

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    BidCount = table.Column<int>(type: "integer", nullable: false),
                    CloseTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CurrentPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ExtensionInterval = table.Column<int>(type: "integer", nullable: false),
                    Grade = table.Column<Grade>(type: "jsonb", nullable: true),
                    InitialCloseTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    InventoryNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false),
                    Location = table.Column<Location>(type: "jsonb", nullable: true),
                    MarketStatus = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    OpenTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OriginType = table.Column<string>(type: "text", nullable: true),
                    Photos = table.Column<List<Photo>>(type: "jsonb", nullable: false),
                    ProjectExtended = table.Column<bool>(type: "boolean", nullable: false),
                    RetailPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    BidCount = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceHistory_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistory_ProductId_RecordedAt",
                table: "PriceHistory",
                columns: new[] { "ProductId", "RecordedAt" });
        }
    }
}
