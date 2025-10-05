using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SantiyeTalepApi.Migrations
{
    /// <inheritdoc />
    public partial class OfferCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Currency",
                table: "Offers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Offers");
        }
    }
}
