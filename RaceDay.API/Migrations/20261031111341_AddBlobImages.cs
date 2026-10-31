using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDay.API.Migrations
{
    /// <inheritdoc />
    public partial class AddBlobImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureUrl",
                table: "Participant",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BannerUrl",
                table: "Event",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilePictureUrl",
                table: "Participant");

            migrationBuilder.DropColumn(
                name: "BannerUrl",
                table: "Event");
        }
    }
}
