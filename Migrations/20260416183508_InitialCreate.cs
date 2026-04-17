using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Category",
                columns: table => new
                {
                    CategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Category", x => x.CategoryID);
                });

            migrationBuilder.CreateTable(
                name: "Customer",
                columns: table => new
                {
                    CustomerID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ShippingAddress = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customer", x => x.CustomerID);
                });

            migrationBuilder.CreateTable(
                name: "ProductLocation",
                columns: table => new
                {
                    LocationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductLocation", x => x.LocationID);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    RoleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.RoleID);
                });

            migrationBuilder.CreateTable(
                name: "Supplier",
                columns: table => new
                {
                    SupplierID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Supplier", x => x.SupplierID);
                });

            migrationBuilder.CreateTable(
                name: "Product",
                columns: table => new
                {
                    ProductID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CategoryID = table.Column<int>(type: "int", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    WarrantyMonths = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => x.ProductID);
                    table.ForeignKey(
                        name: "FK_Product_Category_CategoryID",
                        column: x => x.CategoryID,
                        principalTable: "Category",
                        principalColumn: "CategoryID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleID = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.UserID);
                    table.ForeignKey(
                        name: "FK_User_Role_RoleID",
                        column: x => x.RoleID,
                        principalTable: "Role",
                        principalColumn: "RoleID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Inventory",
                columns: table => new
                {
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    LocationID = table.Column<int>(type: "int", nullable: false),
                    QuantityAvailable = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventory", x => new { x.ProductID, x.LocationID });
                    table.ForeignKey(
                        name: "FK_Inventory_ProductLocation_LocationID",
                        column: x => x.LocationID,
                        principalTable: "ProductLocation",
                        principalColumn: "LocationID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inventory_Product_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Product",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrder",
                columns: table => new
                {
                    POID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpectedDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrder", x => x.POID);
                    table.ForeignKey(
                        name: "FK_PurchaseOrder_Supplier_SupplierID",
                        column: x => x.SupplierID,
                        principalTable: "Supplier",
                        principalColumn: "SupplierID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseOrder_User_UserID",
                        column: x => x.UserID,
                        principalTable: "User",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SaleOrder",
                columns: table => new
                {
                    SOID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleOrder", x => x.SOID);
                    table.ForeignKey(
                        name: "FK_SaleOrder_Customer_CustomerID",
                        column: x => x.CustomerID,
                        principalTable: "Customer",
                        principalColumn: "CustomerID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SaleOrder_User_UserID",
                        column: x => x.UserID,
                        principalTable: "User",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductSerial",
                columns: table => new
                {
                    SerialID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LocationID = table.Column<int>(type: "int", nullable: false),
                    POID = table.Column<int>(type: "int", nullable: false),
                    WarrantyEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
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

            migrationBuilder.CreateTable(
                name: "PurchaseOrderDetail",
                columns: table => new
                {
                    PODetailID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    POID = table.Column<int>(type: "int", nullable: false),
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderDetail", x => x.PODetailID);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderDetail_Product_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Product",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderDetail_PurchaseOrder_POID",
                        column: x => x.POID,
                        principalTable: "PurchaseOrder",
                        principalColumn: "POID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Delivery",
                columns: table => new
                {
                    DeliveryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SOID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    HandShakeProof = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DeliveryTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Delivery", x => x.DeliveryID);
                    table.ForeignKey(
                        name: "FK_Delivery_SaleOrder_SOID",
                        column: x => x.SOID,
                        principalTable: "SaleOrder",
                        principalColumn: "SOID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Delivery_User_UserID",
                        column: x => x.UserID,
                        principalTable: "User",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReturnOrder",
                columns: table => new
                {
                    ReturnID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SOID = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Condition = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnOrder", x => x.ReturnID);
                    table.ForeignKey(
                        name: "FK_ReturnOrder_SaleOrder_SOID",
                        column: x => x.SOID,
                        principalTable: "SaleOrder",
                        principalColumn: "SOID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SaleOrderDetail",
                columns: table => new
                {
                    SODetailID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SOID = table.Column<int>(type: "int", nullable: false),
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleOrderDetail", x => x.SODetailID);
                    table.ForeignKey(
                        name: "FK_SaleOrderDetail_Product_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Product",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleOrderDetail_SaleOrder_SOID",
                        column: x => x.SOID,
                        principalTable: "SaleOrder",
                        principalColumn: "SOID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Delivery_SOID",
                table: "Delivery",
                column: "SOID");

            migrationBuilder.CreateIndex(
                name: "IX_Delivery_UserID",
                table: "Delivery",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_LocationID",
                table: "Inventory",
                column: "LocationID");

            migrationBuilder.CreateIndex(
                name: "IX_Product_CategoryID",
                table: "Product",
                column: "CategoryID");

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

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrder_SupplierID",
                table: "PurchaseOrder",
                column: "SupplierID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrder_UserID",
                table: "PurchaseOrder",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderDetail_POID",
                table: "PurchaseOrderDetail",
                column: "POID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderDetail_ProductID",
                table: "PurchaseOrderDetail",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnOrder_SOID",
                table: "ReturnOrder",
                column: "SOID");

            migrationBuilder.CreateIndex(
                name: "IX_SaleOrder_CustomerID",
                table: "SaleOrder",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_SaleOrder_UserID",
                table: "SaleOrder",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_SaleOrderDetail_ProductID",
                table: "SaleOrderDetail",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_SaleOrderDetail_SOID",
                table: "SaleOrderDetail",
                column: "SOID");

            migrationBuilder.CreateIndex(
                name: "IX_User_RoleID",
                table: "User",
                column: "RoleID");

            migrationBuilder.CreateIndex(
                name: "IX_User_Username",
                table: "User",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Delivery");

            migrationBuilder.DropTable(
                name: "Inventory");

            migrationBuilder.DropTable(
                name: "ProductSerial");

            migrationBuilder.DropTable(
                name: "PurchaseOrderDetail");

            migrationBuilder.DropTable(
                name: "ReturnOrder");

            migrationBuilder.DropTable(
                name: "SaleOrderDetail");

            migrationBuilder.DropTable(
                name: "ProductLocation");

            migrationBuilder.DropTable(
                name: "PurchaseOrder");

            migrationBuilder.DropTable(
                name: "Product");

            migrationBuilder.DropTable(
                name: "SaleOrder");

            migrationBuilder.DropTable(
                name: "Supplier");

            migrationBuilder.DropTable(
                name: "Category");

            migrationBuilder.DropTable(
                name: "Customer");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Role");
        }
    }
}
