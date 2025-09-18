using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SantiyeTalepApi.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnitField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "Requests");

            migrationBuilder.RenameColumn(
                name: "Unit",
                table: "Requests",
                newName: "Quantity");

            migrationBuilder.AlterColumn<string>(
                name: "ProductDescription",
                table: "Requests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "Requests",
                newName: "Unit");

            migrationBuilder.AlterColumn<string>(
                name: "ProductDescription",
                table: "Requests",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Requests",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");
        }
    }
}
