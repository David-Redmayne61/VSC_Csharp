using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirstProject.Migrations
{
    /// <inheritdoc />
    public partial class AddLastModifiedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastModified",
                table: "CustomerContacts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "CustomerContacts",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastModified",
                table: "CustomerContacts");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "CustomerContacts");
        }
    }
}
