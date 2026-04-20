using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Migrations
{
    /// <inheritdoc />
    public partial class FinalERDAlignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RefundAmount",
                table: "PurchaseReturn",
                newName: "Amount");

            migrationBuilder.AddColumn<int>(
                name: "UserID",
                table: "ReturnOrder",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnOrder_UserID",
                table: "ReturnOrder",
                column: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnOrder_User_UserID",
                table: "ReturnOrder",
                column: "UserID",
                principalTable: "User",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReturnOrder_User_UserID",
                table: "ReturnOrder");

            migrationBuilder.DropIndex(
                name: "IX_ReturnOrder_UserID",
                table: "ReturnOrder");

            migrationBuilder.DropColumn(
                name: "UserID",
                table: "ReturnOrder");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "PurchaseReturn",
                newName: "RefundAmount");
        }
    }
}
