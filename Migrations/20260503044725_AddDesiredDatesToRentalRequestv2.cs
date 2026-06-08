using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyPhongTro.Migrations
{
    /// <inheritdoc />
    public partial class AddDesiredDatesToRentalRequestv2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Deposit",
                table: "Contracts",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ElectricMeter",
                table: "Contracts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ManagementFee",
                table: "Contracts",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ParkingFee",
                table: "Contracts",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentDay",
                table: "Contracts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WaterMeter",
                table: "Contracts",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Deposit",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ElectricMeter",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ManagementFee",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ParkingFee",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "PaymentDay",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "WaterMeter",
                table: "Contracts");
        }
    }
}
