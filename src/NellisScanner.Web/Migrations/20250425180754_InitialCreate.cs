using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using NellisScanner.Core.Models;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NellisScanner.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Grade = table.Column<Grade>(type: "jsonb", nullable: true),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InventoryNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Photos = table.Column<List<Photo>>(type: "jsonb", nullable: false),
                    RetailPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    BidCount = table.Column<int>(type: "integer", nullable: false),
                    CurrentPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OpenTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CloseTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    InitialCloseTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false),
                    MarketStatus = table.Column<string>(type: "text", nullable: true),
                    Location = table.Column<Location>(type: "jsonb", nullable: true),
                    OriginType = table.Column<string>(type: "text", nullable: true),
                    ExtensionInterval = table.Column<int>(type: "integer", nullable: false),
                    ProjectExtended = table.Column<bool>(type: "boolean", nullable: false)
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
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BidCount = table.Column<int>(type: "integer", nullable: false),
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriceHistory");

            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
