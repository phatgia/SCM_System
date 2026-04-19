using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePurchaseReturnSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserID",
                table: "PurchaseReturn",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturn_UserID",
                table: "PurchaseReturn",
                column: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseReturn_User_UserID",
                table: "PurchaseReturn",
                column: "UserID",
                principalTable: "User",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseReturn_User_UserID",
                table: "PurchaseReturn");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseReturn_UserID",
                table: "PurchaseReturn");

            migrationBuilder.DropColumn(
                name: "UserID",
                table: "PurchaseReturn");
        }
    }
}
