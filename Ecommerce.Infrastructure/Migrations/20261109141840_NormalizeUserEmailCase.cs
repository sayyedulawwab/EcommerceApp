using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeUserEmailCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Email.Create now stores the lowercase form. Convert the rows saved before that.
            // If two accounts differ only in case, ix_users_email rejects this UPDATE and the migration fails:
            // merge or delete the duplicates first. Acceptable for this sample, which has no such data.
            migrationBuilder.Sql("UPDATE users SET email = lower(email) WHERE email <> lower(email);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The original casing is not kept, so there is nothing to restore.
        }
    }
}
