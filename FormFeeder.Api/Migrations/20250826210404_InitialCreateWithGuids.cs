using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormFeeder.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateWithGuids : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FormConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    PrivacyMode = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FormSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormId = table.Column<string>(type: "text", nullable: true),
                    FormData = table.Column<string>(type: "jsonb", nullable: false),
                    ClientIp = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    Referer = table.Column<string>(type: "text", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormSubmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AllowedDomains",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Domain = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FormConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllowedDomains", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AllowedDomains_FormConfigurations_FormConfigurationId",
                        column: x => x.FormConfigurationId,
                        principalTable: "FormConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConnectorConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    SettingsJson = table.Column<string>(type: "jsonb", nullable: true),
                    FormConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectorConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConnectorConfigurations_FormConfigurations_FormConfiguratio~",
                        column: x => x.FormConfigurationId,
                        principalTable: "FormConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RateLimitSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestsPerWindow = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    WindowMinutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    FormConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateLimitSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RateLimitSettings_FormConfigurations_FormConfigurationId",
                        column: x => x.FormConfigurationId,
                        principalTable: "FormConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AllowedDomains_Domain",
                table: "AllowedDomains",
                column: "Domain");

            migrationBuilder.CreateIndex(
                name: "IX_AllowedDomains_FormConfigurationId_Domain",
                table: "AllowedDomains",
                columns: new[] { "FormConfigurationId", "Domain" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConnectorConfigurations_Enabled",
                table: "ConnectorConfigurations",
                column: "Enabled");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectorConfigurations_FormConfigurationId_Name",
                table: "ConnectorConfigurations",
                columns: new[] { "FormConfigurationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConnectorConfigurations_Type",
                table: "ConnectorConfigurations",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_FormConfigurations_FormId",
                table: "FormConfigurations",
                column: "FormId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissions_FormId",
                table: "FormSubmissions",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissions_FormId_SubmittedAt",
                table: "FormSubmissions",
                columns: new[] { "FormId", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissions_SubmittedAt",
                table: "FormSubmissions",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitSettings_FormConfigurationId",
                table: "RateLimitSettings",
                column: "FormConfigurationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllowedDomains");

            migrationBuilder.DropTable(
                name: "ConnectorConfigurations");

            migrationBuilder.DropTable(
                name: "FormSubmissions");

            migrationBuilder.DropTable(
                name: "RateLimitSettings");

            migrationBuilder.DropTable(
                name: "FormConfigurations");
        }
    }
}
