using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace seed_b_backend.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexesForPropertyDefinitionAndProductProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "PropertyDefinitions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyDefinitions_SubcategoryId_Name",
                table: "PropertyDefinitions",
                columns: new[] { "SubcategoryId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductProperties_ProductId_PropertyDefinitionId",
                table: "ProductProperties",
                columns: new[] { "ProductId", "PropertyDefinitionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PropertyDefinitions_SubcategoryId_Name",
                table: "PropertyDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_ProductProperties_ProductId_PropertyDefinitionId",
                table: "ProductProperties");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "PropertyDefinitions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
