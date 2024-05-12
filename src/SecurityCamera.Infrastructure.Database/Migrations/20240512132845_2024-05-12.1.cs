using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecurityCamera.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class _202405121 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImageDetections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CameraName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ImageName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ImageSize = table.Column<int>(type: "int", nullable: false),
                    DetectionData = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DetectionDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RemoteStorageContainer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RemoteStorageFilePath = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageDetections", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImageDetections");
        }
    }
}
