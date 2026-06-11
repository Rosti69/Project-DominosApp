using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DominosApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAENvONcJXSmuf5CxVVcyyTfAJ+u8kUawn3QogLJwguGzO170R7mR6vMLnmo3fk2+TFw==");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Orders");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id",
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEG8sxFYnxKpXqzwhQLhtmb26qM6yRaXzJnByrnnYFBI3L7qnmRkbhfuWHbSK1H6iSw==");
        }
    }
}
