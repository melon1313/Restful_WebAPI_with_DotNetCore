using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fake.API.Migrations
{
    public partial class DataSeeding : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "TouristRoutes",
                columns: new[] { "Id", "CreateTime", "DepartureTime", "Description", "DiscountPresent", "Features", "Fees", "Notes", "OriginPrice", "Title", "UpdateTime" },
                values: new object[] { new Guid("0d0f5f3a-d1a1-4e74-bdbe-0fbd260adb7e"), new DateTime(2021, 4, 26, 15, 55, 13, 935, DateTimeKind.Utc).AddTicks(3058), null, "shuoming", null, null, null, null, 0m, "ceshititle", null });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "TouristRoutes",
                keyColumn: "Id",
                keyValue: new Guid("0d0f5f3a-d1a1-4e74-bdbe-0fbd260adb7e"));
        }
    }
}
