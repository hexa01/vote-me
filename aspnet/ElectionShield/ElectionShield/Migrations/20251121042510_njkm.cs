using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectionShield.Migrations
{
    /// <inheritdoc />
    public partial class njkm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiAnalysisResult",
                table: "Reports",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiAnalysisResult",
                table: "Reports");
        }
    }
}
