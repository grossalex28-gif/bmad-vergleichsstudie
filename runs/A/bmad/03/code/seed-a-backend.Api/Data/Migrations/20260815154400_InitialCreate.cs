using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace seed_a_backend.Api.Data.Migrations
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
                    Reihen = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Spalten = table.Column<int>(type: "int", nullable: false),
                    GangSpalten = table.Column<string>(type: "nvarchar(max)", nullable: false)
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
                    RaumId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DauerMinuten = table.Column<int>(type: "int", nullable: false),
                    Altersfreigabe = table.Column<int>(type: "int", nullable: false),
                    Zeitpunkt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Veranstaltungen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Veranstaltungen_Raeume_RaumId",
                        column: x => x.RaumId,
                        principalTable: "Raeume",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Preiskategorien",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Preis = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    VeranstaltungId = table.Column<string>(type: "nvarchar(450)", nullable: false)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
