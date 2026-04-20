using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Migrations
{
    /// <inheritdoc />
    public partial class RefactorUserAndReturnSchemaFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Delivery_User_UserID",
                table: "Delivery");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrder_User_UserID",
                table: "PurchaseOrder");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseReturn_User_UserID",
                table: "PurchaseReturn");

            migrationBuilder.DropForeignKey(
                name: "FK_ReturnOrder_User_UserID",
                table: "ReturnOrder");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleOrder_User_UserID",
                table: "SaleOrder");

            migrationBuilder.DropForeignKey(
                name: "FK_User_Role_RoleID",
                table: "User");

            migrationBuilder.DropPrimaryKey(
                name: "PK_User",
                table: "User");

            migrationBuilder.RenameTable(
                name: "User",
                newName: "Users");

            migrationBuilder.RenameColumn(
                name: "Condition",
                table: "ReturnOrder",
                newName: "Settlement");

            migrationBuilder.RenameIndex(
                name: "IX_User_Username",
                table: "Users",
                newName: "IX_Users_Username");

            migrationBuilder.RenameIndex(
                name: "IX_User_RoleID",
                table: "Users",
                newName: "IX_Users_RoleID");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ReturnOrder",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Delivery_Users_UserID",
                table: "Delivery",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrder_Users_UserID",
                table: "PurchaseOrder",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseReturn_Users_UserID",
                table: "PurchaseReturn",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnOrder_Users_UserID",
                table: "ReturnOrder",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleOrder_Users_UserID",
                table: "SaleOrder",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Role_RoleID",
                table: "Users",
                column: "RoleID",
                principalTable: "Role",
                principalColumn: "RoleID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Delivery_Users_UserID",
                table: "Delivery");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrder_Users_UserID",
                table: "PurchaseOrder");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseReturn_Users_UserID",
                table: "PurchaseReturn");

            migrationBuilder.DropForeignKey(
                name: "FK_ReturnOrder_Users_UserID",
                table: "ReturnOrder");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleOrder_Users_UserID",
                table: "SaleOrder");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Role_RoleID",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ReturnOrder");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "User");

            migrationBuilder.RenameColumn(
                name: "Settlement",
                table: "ReturnOrder",
                newName: "Condition");

            migrationBuilder.RenameIndex(
                name: "IX_Users_Username",
                table: "User",
                newName: "IX_User_Username");

            migrationBuilder.RenameIndex(
                name: "IX_Users_RoleID",
                table: "User",
                newName: "IX_User_RoleID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_User",
                table: "User",
                column: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Delivery_User_UserID",
                table: "Delivery",
                column: "UserID",
                principalTable: "User",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrder_User_UserID",
                table: "PurchaseOrder",
                column: "UserID",
                principalTable: "User",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseReturn_User_UserID",
                table: "PurchaseReturn",
                column: "UserID",
                principalTable: "User",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnOrder_User_UserID",
                table: "ReturnOrder",
                column: "UserID",
                principalTable: "User",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleOrder_User_UserID",
                table: "SaleOrder",
                column: "UserID",
                principalTable: "User",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_User_Role_RoleID",
                table: "User",
                column: "RoleID",
                principalTable: "Role",
                principalColumn: "RoleID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
