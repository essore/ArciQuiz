using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrasctructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Domande",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Categoria = table.Column<string>(type: "TEXT", nullable: false),
                    Difficolta = table.Column<string>(type: "TEXT", nullable: false),
                    Testo = table.Column<string>(type: "TEXT", nullable: false),
                    RispostaA = table.Column<string>(type: "TEXT", nullable: false),
                    RispostaB = table.Column<string>(type: "TEXT", nullable: false),
                    RispostaC = table.Column<string>(type: "TEXT", nullable: false),
                    RispostaD = table.Column<string>(type: "TEXT", nullable: false),
                    RispostaEsatta = table.Column<char>(type: "TEXT", nullable: false),
                    FlagErrore = table.Column<bool>(type: "INTEGER", nullable: false),
                    FlgDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Domande", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Partite",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DtCreazione = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Stato = table.Column<int>(type: "INTEGER", nullable: false),
                    Titolo = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partite", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NomeSquadra = table.Column<string>(type: "TEXT", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Manches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PartitaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Stato = table.Column<int>(type: "INTEGER", nullable: false),
                    TempoRispostaSecondi = table.Column<int>(type: "INTEGER", nullable: false),
                    PuntiBase = table.Column<int>(type: "INTEGER", nullable: false),
                    PenalitaErrore = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaxAstensioni = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Manches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Manches_Partite_PartitaId",
                        column: x => x.PartitaId,
                        principalTable: "Partite",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayersPartite",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    PartitaId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayersPartite", x => new { x.PlayerId, x.PartitaId });
                    table.ForeignKey(
                        name: "FK_PlayersPartite_Partite_PartitaId",
                        column: x => x.PartitaId,
                        principalTable: "Partite",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayersPartite_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ManchesDomande",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MancheId = table.Column<int>(type: "INTEGER", nullable: false),
                    DomandaId = table.Column<int>(type: "INTEGER", nullable: true),
                    Index = table.Column<int>(type: "INTEGER", nullable: false),
                    ModificatorePunti = table.Column<int>(type: "INTEGER", nullable: false),
                    DtStart = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DtEnd = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManchesDomande", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManchesDomande_Domande_DomandaId",
                        column: x => x.DomandaId,
                        principalTable: "Domande",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ManchesDomande_Manches_MancheId",
                        column: x => x.MancheId,
                        principalTable: "Manches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ManchesRisposte",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    MancheDomandaId = table.Column<int>(type: "INTEGER", nullable: false),
                    DtRisposta = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Risposta = table.Column<char>(type: "TEXT", nullable: false),
                    IsCorrect = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManchesRisposte", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManchesRisposte_ManchesDomande_MancheDomandaId",
                        column: x => x.MancheDomandaId,
                        principalTable: "ManchesDomande",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ManchesRisposte_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Manches_PartitaId",
                table: "Manches",
                column: "PartitaId");

            migrationBuilder.CreateIndex(
                name: "IX_ManchesDomande_DomandaId",
                table: "ManchesDomande",
                column: "DomandaId");

            migrationBuilder.CreateIndex(
                name: "IX_ManchesDomande_MancheId",
                table: "ManchesDomande",
                column: "MancheId");

            migrationBuilder.CreateIndex(
                name: "IX_ManchesRisposte_MancheDomandaId",
                table: "ManchesRisposte",
                column: "MancheDomandaId");

            migrationBuilder.CreateIndex(
                name: "IX_ManchesRisposte_PlayerId",
                table: "ManchesRisposte",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayersPartite_PartitaId",
                table: "PlayersPartite",
                column: "PartitaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManchesRisposte");

            migrationBuilder.DropTable(
                name: "PlayersPartite");

            migrationBuilder.DropTable(
                name: "ManchesDomande");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Domande");

            migrationBuilder.DropTable(
                name: "Manches");

            migrationBuilder.DropTable(
                name: "Partite");
        }
    }
}
