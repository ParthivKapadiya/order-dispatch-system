using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToplandERP.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase1MasterData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_CompanyId_Code",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_PaymentConditions_CompanyId_Name",
                table: "PaymentConditions");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Transporters",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPerson",
                table: "Transporters",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Products",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelNumber",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "Products",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Customers",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Customers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingCity",
                table: "Customers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BillingPincode",
                table: "Customers",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BillingState",
                table: "Customers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerCode",
                table: "Customers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddress",
                table: "Customers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryCity",
                table: "Customers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryPincode",
                table: "Customers",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryState",
                table: "Customers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeCode",
                table: "AspNetUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CompanyId_Category",
                table: "Products",
                columns: new[] { "CompanyId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_CompanyId_Code",
                table: "Products",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_CompanyId_ModelNumber",
                table: "Products",
                columns: new[] { "CompanyId", "ModelNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_CompanyId_Name",
                table: "Products",
                columns: new[] { "CompanyId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentConditions_CompanyId_Name",
                table: "PaymentConditions",
                columns: new[] { "CompanyId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CompanyId_CustomerCode",
                table: "Customers",
                columns: new[] { "CompanyId", "CustomerCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CompanyId_Phone",
                table: "Customers",
                columns: new[] { "CompanyId", "Phone" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CompanyId_EmployeeCode",
                table: "AspNetUsers",
                columns: new[] { "CompanyId", "EmployeeCode" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_CompanyId_Category",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_CompanyId_Code",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_CompanyId_ModelNumber",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_CompanyId_Name",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_PaymentConditions_CompanyId_Name",
                table: "PaymentConditions");

            migrationBuilder.DropIndex(
                name: "IX_Customers_CompanyId_CustomerCode",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_CompanyId_Phone",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CompanyId_EmployeeCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Transporters");

            migrationBuilder.DropColumn(
                name: "ContactPerson",
                table: "Transporters");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ModelNumber",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "BillingCity",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "BillingPincode",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "BillingState",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CustomerCode",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DeliveryAddress",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DeliveryCity",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DeliveryPincode",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DeliveryState",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "EmployeeCode",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Products",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Customers",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Customers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.CreateIndex(
                name: "IX_Products_CompanyId_Code",
                table: "Products",
                columns: new[] { "CompanyId", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentConditions_CompanyId_Name",
                table: "PaymentConditions",
                columns: new[] { "CompanyId", "Name" });
        }
    }
}
