using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Game.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "accounts",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                password_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_accounts", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "characters",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                account_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                normalized_name = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                class_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                level = table.Column<int>(type: "integer", nullable: false),
                experience = table.Column<long>(type: "bigint", nullable: false),
                zone_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                position_x = table.Column<float>(type: "real", nullable: false),
                position_y = table.Column<float>(type: "real", nullable: false),
                progression_version = table.Column<int>(type: "integer", nullable: false),
                progression = table.Column<string>(type: "jsonb", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_characters", x => x.id);
                table.ForeignKey(
                    name: "FK_characters_accounts_account_id",
                    column: x => x.account_id,
                    principalTable: "accounts",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_accounts_email",
            table: "accounts",
            column: "email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_characters_account_id",
            table: "characters",
            column: "account_id");

        migrationBuilder.CreateIndex(
            name: "IX_characters_normalized_name",
            table: "characters",
            column: "normalized_name",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "characters");

        migrationBuilder.DropTable(
            name: "accounts");
    }
}
