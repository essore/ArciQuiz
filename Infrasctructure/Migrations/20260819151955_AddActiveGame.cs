using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrasctructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActiveGame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAttiva",
                table: "Partite",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Partite_IsAttiva",
                table: "Partite",
                column: "IsAttiva",
                unique: true,
                filter: "\"IsAttiva\" = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Partite_IsAttiva",
                table: "Partite");

            migrationBuilder.DropColumn(
                name: "IsAttiva",
                table: "Partite");
        }
    }
}
