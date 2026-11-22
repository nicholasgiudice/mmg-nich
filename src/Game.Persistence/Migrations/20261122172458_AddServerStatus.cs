using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Game.Persistence.Migrations;

/// <inheritdoc />
public partial class AddServerStatus : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "server_status",
            columns: table => new
            {
                server_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                online_players = table.Column<int>(type: "integer", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_server_status", x => x.server_id);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "server_status");
    }
}
