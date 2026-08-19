using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace seed_a_backend.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSitzplatzbelegung : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sitzplatzbelegungen",
                columns: table => new
                {
                    VeranstaltungId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SitzplatzCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BuchungId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sitzplatzbelegungen", x => new { x.VeranstaltungId, x.SitzplatzCode });
                    table.ForeignKey(
                        name: "FK_Sitzplatzbelegungen_Veranstaltungen_VeranstaltungId",
                        column: x => x.VeranstaltungId,
                        principalTable: "Veranstaltungen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Sitzplatzbelegungen");
        }
    }
}
