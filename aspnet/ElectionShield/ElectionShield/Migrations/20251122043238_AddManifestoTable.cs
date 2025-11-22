using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectionShield.Migrations
{
    /// <inheritdoc />
    public partial class AddManifestoTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiTag",
                table: "Reports",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Reports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Manifestos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fulfilled = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Unfulfilled = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PoliticalName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Manifestos", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Manifestos");

            migrationBuilder.DropColumn(
                name: "AiTag",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Reports");
        }
    }
}
