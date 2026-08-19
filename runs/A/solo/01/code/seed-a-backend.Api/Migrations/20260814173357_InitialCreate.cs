using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace seed_a_backend.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Spielstaetten",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Spielstaetten", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Raeume",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SpielstaetteId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ReihenCsv = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Spalten = table.Column<int>(type: "int", nullable: false),
                    GangSpaltenCsv = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GangHinweis = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Raeume", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Raeume_Spielstaetten_SpielstaetteId",
                        column: x => x.SpielstaetteId,
                        principalTable: "Spielstaetten",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Veranstaltungen",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Titel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Beschreibung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SpielstaetteId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RaumId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Zeitpunkt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DauerMinuten = table.Column<int>(type: "int", nullable: false),
                    Altersfreigabe = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Veranstaltungen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Veranstaltungen_Raeume_RaumId",
                        column: x => x.RaumId,
                        principalTable: "Raeume",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Veranstaltungen_Spielstaetten_SpielstaetteId",
                        column: x => x.SpielstaetteId,
                        principalTable: "Spielstaetten",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Buchungen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Referenz = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErstelltAm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    VeranstaltungId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buchungen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Buchungen_Veranstaltungen_VeranstaltungId",
                        column: x => x.VeranstaltungId,
                        principalTable: "Veranstaltungen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Preiskategorien",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VeranstaltungId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Preis = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Preiskategorien", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Preiskategorien_Veranstaltungen_VeranstaltungId",
                        column: x => x.VeranstaltungId,
                        principalTable: "Veranstaltungen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Buchungspositionen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BuchungId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VeranstaltungId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Reihe = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Spalte = table.Column<int>(type: "int", nullable: false),
                    PreiskategorieId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Preis = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buchungspositionen", x => x.Id);
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
                    table.ForeignKey(
                        name: "FK_Buchungspositionen_Veranstaltungen_VeranstaltungId",
                        column: x => x.VeranstaltungId,
                        principalTable: "Veranstaltungen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Buchung_Referenz",
                table: "Buchungen",
                column: "Referenz",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Buchungen_VeranstaltungId",
                table: "Buchungen",
                column: "VeranstaltungId");

            migrationBuilder.CreateIndex(
                name: "IX_Buchungsposition_AktiverSitzplatz",
                table: "Buchungspositionen",
                columns: new[] { "VeranstaltungId", "Reihe", "Spalte" },
                unique: true,
                filter: "[Status] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Buchungspositionen_BuchungId",
                table: "Buchungspositionen",
                column: "BuchungId");

            migrationBuilder.CreateIndex(
                name: "IX_Buchungspositionen_PreiskategorieId",
                table: "Buchungspositionen",
                column: "PreiskategorieId");

            migrationBuilder.CreateIndex(
                name: "IX_Preiskategorien_VeranstaltungId",
                table: "Preiskategorien",
                column: "VeranstaltungId");

            migrationBuilder.CreateIndex(
                name: "IX_Raeume_SpielstaetteId",
                table: "Raeume",
                column: "SpielstaetteId");

            migrationBuilder.CreateIndex(
                name: "IX_Veranstaltungen_RaumId",
                table: "Veranstaltungen",
                column: "RaumId");

            migrationBuilder.CreateIndex(
                name: "IX_Veranstaltungen_SpielstaetteId",
                table: "Veranstaltungen",
                column: "SpielstaetteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Buchungspositionen");

            migrationBuilder.DropTable(
                name: "Buchungen");

            migrationBuilder.DropTable(
                name: "Preiskategorien");

            migrationBuilder.DropTable(
                name: "Veranstaltungen");

            migrationBuilder.DropTable(
                name: "Raeume");

            migrationBuilder.DropTable(
                name: "Spielstaetten");
        }
    }
}
