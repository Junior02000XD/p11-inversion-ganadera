using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace P11.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Sprint2Inversiones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CierreRondaEn",
                table: "Proyectos",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostosPct",
                table: "Proyectos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MortalidadPct",
                table: "Proyectos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NivelRiesgo",
                table: "Proyectos",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PublicadoEn",
                table: "Proyectos",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RendimientoPct",
                table: "Proyectos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Riesgos",
                table: "Proyectos",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VariacionPct",
                table: "Proyectos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Inversiones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProyectoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InversionistaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MontoBs = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreadaEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReservadaHasta = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ContratoVersion = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ContratoAceptadoEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ContratoAceptadoPor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ReferenciaPago = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    PagadaEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ParticipacionPct = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    ParticipacionKg = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CodigoConstancia = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inversiones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inversiones_AspNetUsers_InversionistaId",
                        column: x => x.InversionistaId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inversiones_Proyectos_ProyectoId",
                        column: x => x.ProyectoId,
                        principalTable: "Proyectos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inversiones_InversionistaId",
                table: "Inversiones",
                column: "InversionistaId");

            migrationBuilder.CreateIndex(
                name: "IX_Inversiones_ProyectoId",
                table: "Inversiones",
                column: "ProyectoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Inversiones");

            migrationBuilder.DropColumn(
                name: "CierreRondaEn",
                table: "Proyectos");

            migrationBuilder.DropColumn(
                name: "CostosPct",
                table: "Proyectos");

            migrationBuilder.DropColumn(
                name: "MortalidadPct",
                table: "Proyectos");

            migrationBuilder.DropColumn(
                name: "NivelRiesgo",
                table: "Proyectos");

            migrationBuilder.DropColumn(
                name: "PublicadoEn",
                table: "Proyectos");

            migrationBuilder.DropColumn(
                name: "RendimientoPct",
                table: "Proyectos");

            migrationBuilder.DropColumn(
                name: "Riesgos",
                table: "Proyectos");

            migrationBuilder.DropColumn(
                name: "VariacionPct",
                table: "Proyectos");
        }
    }
}
