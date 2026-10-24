using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RestrictCategoryDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_products_categories_category_id",
                table: "products");

            migrationBuilder.AddForeignKey(
                name: "fk_products_categories_category_id",
                table: "products",
                column: "category_id",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_products_categories_category_id",
                table: "products");

            migrationBuilder.AddForeignKey(
                name: "fk_products_categories_category_id",
                table: "products",
                column: "category_id",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
