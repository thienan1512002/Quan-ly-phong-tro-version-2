using Microsoft.EntityFrameworkCore.Migrations;
namespace QuanLyPhongTro.Migrations;
public partial class AddElectricityCollection : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>("ContractId", "UtilityReadings", nullable: true);
        migrationBuilder.AddColumn<DateTime>("ElectricityPaidAt", "UtilityReadings", nullable: true);
        migrationBuilder.AddColumn<decimal>("PreviousElectricityMeter", "UtilityReadings", type: "decimal(18,2)", nullable: true);
        migrationBuilder.AddColumn<decimal>("CurrentElectricityMeter", "UtilityReadings", type: "decimal(18,2)", nullable: true);
        migrationBuilder.AddColumn<decimal>("ElectricityUnitPrice", "UtilityReadings", type: "decimal(18,2)", nullable: true);
        migrationBuilder.CreateIndex("IX_UtilityReadings_ContractId", "UtilityReadings", "ContractId");
        migrationBuilder.AddForeignKey("FK_UtilityReadings_Contracts_ContractId", "UtilityReadings", "ContractId", "Contracts", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_UtilityReadings_Contracts_ContractId", "UtilityReadings");
        migrationBuilder.DropIndex("IX_UtilityReadings_ContractId", "UtilityReadings");
        migrationBuilder.DropColumn("ContractId", "UtilityReadings");
        migrationBuilder.DropColumn("ElectricityPaidAt", "UtilityReadings");
        migrationBuilder.DropColumn("PreviousElectricityMeter", "UtilityReadings");
        migrationBuilder.DropColumn("CurrentElectricityMeter", "UtilityReadings");
        migrationBuilder.DropColumn("ElectricityUnitPrice", "UtilityReadings");
    }
}
