using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddReservedQuantityColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ADD COLUMN IF NOT EXISTS: safe on databases that already have the column
            // (created via EnsureCreated before migrations were introduced) as well as
            // fresh databases where InitialCreate's CREATE TABLE IF NOT EXISTS skipped it.
            migrationBuilder.Sql(@"
                ALTER TABLE products
                ADD COLUMN IF NOT EXISTS reserved_quantity integer NOT NULL DEFAULT 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "reserved_quantity",
                table: "products");
        }
    }
}
