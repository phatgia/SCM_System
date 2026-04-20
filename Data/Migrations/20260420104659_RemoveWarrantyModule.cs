using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWarrantyModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductSerial");

            migrationBuilder.DropColumn(
                name: "WarrantyAlertDays",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "WarrantyMonths",
                table: "Product");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WarrantyAlertDays",
                table: "SystemSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WarrantyMonths",
                table: "Product",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductSerial",
                columns: table => new
                {
                    SerialID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationID = table.Column<int>(type: "int", nullable: false),
                    POID = table.Column<int>(type: "int", nullable: false),
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    WarrantyEndDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSerial", x => x.SerialID);
                    table.ForeignKey(
                        name: "FK_ProductSerial_ProductLocation_LocationID",
                        column: x => x.LocationID,
                        principalTable: "ProductLocation",
                        principalColumn: "LocationID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductSerial_Product_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Product",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductSerial_PurchaseOrder_POID",
                        column: x => x.POID,
                        principalTable: "PurchaseOrder",
                        principalColumn: "POID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductSerial_LocationID",
                table: "ProductSerial",
                column: "LocationID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSerial_POID",
                table: "ProductSerial",
                column: "POID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSerial_ProductID",
                table: "ProductSerial",
                column: "ProductID");
        }
    }
}
