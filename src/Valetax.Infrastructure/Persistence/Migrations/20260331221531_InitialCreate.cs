using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Valetax.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "exception_journals",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExceptionType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Path = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Method = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    QueryParameters = table.Column<string>(type: "jsonb", nullable: true),
                    BodyParameters = table.Column<string>(type: "jsonb", nullable: true),
                    StackTrace = table.Column<string>(type: "text", nullable: true),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exception_journals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "trees",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "nodes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TreeId = table.Column<long>(type: "bigint", nullable: false),
                    ParentId = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_nodes_nodes_ParentId",
                        column: x => x.ParentId,
                        principalTable: "nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_nodes_trees_TreeId",
                        column: x => x.TreeId,
                        principalTable: "trees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_exception_journals_CreatedAt",
                table: "exception_journals",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_exception_journals_EventId",
                table: "exception_journals",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_nodes_ParentId",
                table: "nodes",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "UX_nodes_tree_root",
                table: "nodes",
                column: "TreeId",
                unique: true,
                filter: "\"ParentId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_nodes_tree_parent_name",
                table: "nodes",
                columns: new[] { "TreeId", "ParentId", "Name" },
                unique: true,
                filter: "\"ParentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_trees_Name",
                table: "trees",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exception_journals");

            migrationBuilder.DropTable(
                name: "nodes");

            migrationBuilder.DropTable(
                name: "trees");
        }
    }
}
