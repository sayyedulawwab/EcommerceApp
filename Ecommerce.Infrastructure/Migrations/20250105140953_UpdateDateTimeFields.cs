using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDateTimeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_products_categories_product_category_id",
                table: "products");

            migrationBuilder.RenameColumn(
                name: "product_category_id",
                table: "products",
                newName: "category_id");

            migrationBuilder.RenameIndex(
                name: "ix_products_product_category_id",
                table: "products",
                newName: "ix_products_category_id");

            migrationBuilder.AddForeignKey(
                name: "fk_products_categories_category_id",
                table: "products",
                column: "category_id",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_products_categories_category_id",
                table: "products");

            migrationBuilder.RenameColumn(
                name: "category_id",
                table: "products",
                newName: "product_category_id");

            migrationBuilder.RenameIndex(
                name: "ix_products_category_id",
                table: "products",
                newName: "ix_products_product_category_id");

            migrationBuilder.AddForeignKey(
                name: "fk_products_categories_product_category_id",
                table: "products",
                column: "product_category_id",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
