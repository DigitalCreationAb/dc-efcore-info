using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ef_core_essentials_blazor.Migrations
{
    /// <inheritdoc />
    public partial class Step2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Host",
                table: "Sites",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sites_Host",
                table: "Sites",
                column: "Host");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sites_Host",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "Host",
                table: "Sites");
        }
    }
}
