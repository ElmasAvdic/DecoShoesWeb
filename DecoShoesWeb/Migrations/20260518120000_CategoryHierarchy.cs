using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DecoShoesWeb.Migrations
{
    public partial class CategoryHierarchy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentCategoryID",
                table: "Categories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Categories",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "Categories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Categories",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Products",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentCategoryID",
                table: "Categories",
                column: "ParentCategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                table: "Categories",
                column: "Slug",
                unique: true,
                filter: "[Slug] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Categories_ParentCategoryID",
                table: "Categories",
                column: "ParentCategoryID",
                principalTable: "Categories",
                principalColumn: "CategoryID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = 'zenske')
    INSERT INTO Categories (Name, Description, ParentCategoryID, Slug, DisplayOrder, IsActive) VALUES (N'Ženske', N'Glavna ženska kategorija', NULL, 'zenske', 10, 1);
IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = 'muski')
    INSERT INTO Categories (Name, Description, ParentCategoryID, Slug, DisplayOrder, IsActive) VALUES (N'Muški', N'Glavna muška kategorija', NULL, 'muski', 20, 1);
IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = 'djeca')
    INSERT INTO Categories (Name, Description, ParentCategoryID, Slug, DisplayOrder, IsActive) VALUES (N'Djeca', N'Dječija kategorija', NULL, 'djeca', 30, 1);

DECLARE @ZenskeId int = (SELECT TOP 1 CategoryID FROM Categories WHERE Slug = 'zenske');
DECLARE @MuskiId int = (SELECT TOP 1 CategoryID FROM Categories WHERE Slug = 'muski');
DECLARE @DjecaId int = (SELECT TOP 1 CategoryID FROM Categories WHERE Slug = 'djeca');

IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = 'zenske-patike')
    INSERT INTO Categories (Name, Description, ParentCategoryID, Slug, DisplayOrder, IsActive) VALUES (N'Ženske patike', NULL, @ZenskeId, 'zenske-patike', 10, 1);
IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = 'zenske-cipele')
    INSERT INTO Categories (Name, Description, ParentCategoryID, Slug, DisplayOrder, IsActive) VALUES (N'Ženske cipele', NULL, @ZenskeId, 'zenske-cipele', 20, 1);
IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = 'zenske-cizme')
    INSERT INTO Categories (Name, Description, ParentCategoryID, Slug, DisplayOrder, IsActive) VALUES (N'Ženske čizme', NULL, @ZenskeId, 'zenske-cizme', 30, 1);
IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = 'zenske-sandale')
    INSERT INTO Categories (Name, Description, ParentCategoryID, Slug, DisplayOrder, IsActive) VALUES (N'Ženske sandale', NULL, @ZenskeId, 'zenske-sandale', 40, 1);
IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = 'zenske-torbe')
    INSERT INTO Categories (Name, Description, ParentCategoryID, Slug, DisplayOrder, IsActive) VALUES (N'Ženske torbe', NULL, @ZenskeId, 'zenske-torbe', 50, 1);

IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = 'muske-patike')
    INSERT INTO Categories (Name, Description, ParentCategoryID, Slug, DisplayOrder, IsActive) VALUES (N'Muške patike', NULL, @MuskiId, 'muske-patike', 10, 1);
IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = 'muske-cipele')
    INSERT INTO Categories (Name, Description, ParentCategoryID, Slug, DisplayOrder, IsActive) VALUES (N'Muške cipele', NULL, @MuskiId, 'muske-cipele', 20, 1);
IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = 'muske-sandale')
    INSERT INTO Categories (Name, Description, ParentCategoryID, Slug, DisplayOrder, IsActive) VALUES (N'Muške sandale', NULL, @MuskiId, 'muske-sandale', 40, 1);
IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = 'muske-torbe')
    INSERT INTO Categories (Name, Description, ParentCategoryID, Slug, DisplayOrder, IsActive) VALUES (N'Muške torbe', NULL, @MuskiId, 'muske-torbe', 50, 1);

IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = 'djecije-patike')
    INSERT INTO Categories (Name, Description, ParentCategoryID, Slug, DisplayOrder, IsActive) VALUES (N'Dječije patike', NULL, @DjecaId, 'djecije-patike', 10, 1);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Categories_ParentCategoryID",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_ParentCategoryID",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Slug",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "ParentCategoryID",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Products");
        }
    }
}
