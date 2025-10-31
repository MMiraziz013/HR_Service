using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clean.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ModifiedEmployeeVacationRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_vacation_balances_EmployeeId",
                table: "vacation_balances");

            migrationBuilder.CreateIndex(
                name: "IX_vacation_balances_EmployeeId",
                table: "vacation_balances",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_vacation_balances_Year",
                table: "vacation_balances",
                column: "Year");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_vacation_balances_EmployeeId",
                table: "vacation_balances");

            migrationBuilder.DropIndex(
                name: "IX_vacation_balances_Year",
                table: "vacation_balances");

            migrationBuilder.CreateIndex(
                name: "IX_vacation_balances_EmployeeId",
                table: "vacation_balances",
                column: "EmployeeId",
                unique: true);
        }
    }
}
