using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace seed_a_backend.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBuchungUndBuchungsposition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Buchungen",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Referenz = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VeranstaltungId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Gesamtpreis = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buchungen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Buchungen_Veranstaltungen_VeranstaltungId",
                        column: x => x.VeranstaltungId,
                        principalTable: "Veranstaltungen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Buchungspositionen",
                columns: table => new
                {
                    BuchungId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SitzplatzCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PreiskategorieId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PreisSnapshot = table.Column<decimal>(type: "decimal(6,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buchungspositionen", x => new { x.BuchungId, x.SitzplatzCode });
                    table.ForeignKey(
                        name: "FK_Buchungspositionen_Buchungen_BuchungId",
                        column: x => x.BuchungId,
                        principalTable: "Buchungen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Buchungspositionen_Preiskategorien_PreiskategorieId",
                        column: x => x.PreiskategorieId,
                        principalTable: "Preiskategorien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sitzplatzbelegungen_BuchungId",
                table: "Sitzplatzbelegungen",
                column: "BuchungId");

            migrationBuilder.CreateIndex(
                name: "IX_Buchungen_Referenz",
                table: "Buchungen",
                column: "Referenz",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Buchungen_VeranstaltungId",
                table: "Buchungen",
                column: "VeranstaltungId");

            migrationBuilder.CreateIndex(
                name: "IX_Buchungspositionen_PreiskategorieId",
                table: "Buchungspositionen",
                column: "PreiskategorieId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sitzplatzbelegungen_Buchungen_BuchungId",
                table: "Sitzplatzbelegungen",
                column: "BuchungId",
                principalTable: "Buchungen",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sitzplatzbelegungen_Buchungen_BuchungId",
                table: "Sitzplatzbelegungen");

            migrationBuilder.DropTable(
                name: "Buchungspositionen");

            migrationBuilder.DropTable(
                name: "Buchungen");

            migrationBuilder.DropIndex(
                name: "IX_Sitzplatzbelegungen_BuchungId",
                table: "Sitzplatzbelegungen");
        }
    }
}
