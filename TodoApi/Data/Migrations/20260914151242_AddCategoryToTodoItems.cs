using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryToTodoItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "TodoItems",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Categories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_CategoryId",
                table: "TodoItems",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_TodoItems_Categories_CategoryId",
                table: "TodoItems",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TodoItems_Categories_CategoryId",
                table: "TodoItems");

            migrationBuilder.DropIndex(
                name: "IX_TodoItems_CategoryId",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "TodoItems");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
