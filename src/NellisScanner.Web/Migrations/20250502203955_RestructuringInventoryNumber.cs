using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NellisScanner.Web.Migrations
{
    /// <inheritdoc />
    public partial class RestructuringInventoryNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Auctions_Inventory_InventoryNumber",
                table: "Auctions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Inventory_InventoryNumber",
                table: "Inventory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Inventory",
                table: "Inventory");

            migrationBuilder.DropIndex(
                name: "IX_Inventory_InventoryNumber",
                table: "Inventory");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Inventory");

            migrationBuilder.AlterColumn<long>(
                name: "InventoryNumber",
                table: "Inventory",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "InventoryNumber",
                table: "Auctions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Inventory",
                table: "Inventory",
                column: "InventoryNumber");

            migrationBuilder.AddForeignKey(
                name: "FK_Auctions_Inventory_InventoryNumber",
                table: "Auctions",
                column: "InventoryNumber",
                principalTable: "Inventory",
                principalColumn: "InventoryNumber",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Auctions_Inventory_InventoryNumber",
                table: "Auctions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Inventory",
                table: "Inventory");

            migrationBuilder.AlterColumn<string>(
                name: "InventoryNumber",
                table: "Inventory",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Inventory",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<string>(
                name: "InventoryNumber",
                table: "Auctions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Inventory_InventoryNumber",
                table: "Inventory",
                column: "InventoryNumber");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Inventory",
                table: "Inventory",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_InventoryNumber",
                table: "Inventory",
                column: "InventoryNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Auctions_Inventory_InventoryNumber",
                table: "Auctions",
                column: "InventoryNumber",
                principalTable: "Inventory",
                principalColumn: "InventoryNumber",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
