using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Geniapp.Infrastructure.Database.MasterDatabase.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Shards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantShardAssociations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShardId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantShardAssociations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantShardAssociations_Shards_ShardId",
                        column: x => x.ShardId,
                        principalTable: "Shards",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantShardAssociations_ShardId",
                table: "TenantShardAssociations",
                column: "ShardId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantShardAssociations_TenantId",
                table: "TenantShardAssociations",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantShardAssociations");

            migrationBuilder.DropTable(
                name: "Shards");
        }
    }
}
