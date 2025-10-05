using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SantiyeTalepApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOfferModelForEnhancedQuotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "Offers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DeliveryType",
                table: "Offers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Discount",
                table: "Offers",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Offers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Brand",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "DeliveryType",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "Discount",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Offers");
        }
    }
}
