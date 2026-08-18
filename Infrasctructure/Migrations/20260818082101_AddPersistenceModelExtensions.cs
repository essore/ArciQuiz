using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrasctructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPersistenceModelExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ManchesDomande_Domande_DomandaId",
                table: "ManchesDomande");

            migrationBuilder.DropIndex(
                name: "IX_ManchesRisposte_PlayerId",
                table: "ManchesRisposte");

            migrationBuilder.AddColumn<DateTime>(
                name: "DtIscrizioneUtc",
                table: "PlayersPartite",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DtUltimoAccessoUtc",
                table: "PlayersPartite",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SessionToken",
                table: "PlayersPartite",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DtIscrizioneUtc",
                table: "Players",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DtUltimoAccessoUtc",
                table: "Players",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PartitaId",
                table: "Players",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SessionToken",
                table: "Players",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentMancheDomandaId",
                table: "Partite",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentMancheId",
                table: "Partite",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DtFineUtc",
                table: "Partite",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DtInizioUtc",
                table: "Partite",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<char>(
                name: "Risposta",
                table: "ManchesRisposte",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(char),
                oldType: "TEXT");

            migrationBuilder.AddColumn<bool>(
                name: "AstensionePenalizzata",
                table: "ManchesRisposte",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "CoefficienteTempo",
                table: "ManchesRisposte",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAnnullata",
                table: "ManchesRisposte",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsAstenuto",
                table: "ManchesRisposte",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PuntiAssegnati",
                table: "ManchesRisposte",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TempoImpiegatoMs",
                table: "ManchesRisposte",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DtAnnullamentoUtc",
                table: "ManchesDomande",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurataSecondiOverride",
                table: "ManchesDomande",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAnnullata",
                table: "ManchesDomande",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MotivoAnnullamento",
                table: "ManchesDomande",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ScadenzaUtc",
                table: "ManchesDomande",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DtFineUtc",
                table: "Manches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DtInizioUtc",
                table: "Manches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MalusBase",
                table: "Manches",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Moltiplicatore",
                table: "Manches",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Ordine",
                table: "Manches",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Players_PartitaId_NomeSquadra",
                table: "Players",
                columns: new[] { "PartitaId", "NomeSquadra" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ManchesRisposte_PlayerId_MancheDomandaId",
                table: "ManchesRisposte",
                columns: new[] { "PlayerId", "MancheDomandaId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ManchesDomande_Domande_DomandaId",
                table: "ManchesDomande",
                column: "DomandaId",
                principalTable: "Domande",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Partite_PartitaId",
                table: "Players",
                column: "PartitaId",
                principalTable: "Partite",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ManchesDomande_Domande_DomandaId",
                table: "ManchesDomande");

            migrationBuilder.DropForeignKey(
                name: "FK_Players_Partite_PartitaId",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Players_PartitaId_NomeSquadra",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_ManchesRisposte_PlayerId_MancheDomandaId",
                table: "ManchesRisposte");

            migrationBuilder.DropColumn(
                name: "DtIscrizioneUtc",
                table: "PlayersPartite");

            migrationBuilder.DropColumn(
                name: "DtUltimoAccessoUtc",
                table: "PlayersPartite");

            migrationBuilder.DropColumn(
                name: "SessionToken",
                table: "PlayersPartite");

            migrationBuilder.DropColumn(
                name: "DtIscrizioneUtc",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "DtUltimoAccessoUtc",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "PartitaId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "SessionToken",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "CurrentMancheDomandaId",
                table: "Partite");

            migrationBuilder.DropColumn(
                name: "CurrentMancheId",
                table: "Partite");

            migrationBuilder.DropColumn(
                name: "DtFineUtc",
                table: "Partite");

            migrationBuilder.DropColumn(
                name: "DtInizioUtc",
                table: "Partite");

            migrationBuilder.DropColumn(
                name: "AstensionePenalizzata",
                table: "ManchesRisposte");

            migrationBuilder.DropColumn(
                name: "CoefficienteTempo",
                table: "ManchesRisposte");

            migrationBuilder.DropColumn(
                name: "IsAnnullata",
                table: "ManchesRisposte");

            migrationBuilder.DropColumn(
                name: "IsAstenuto",
                table: "ManchesRisposte");

            migrationBuilder.DropColumn(
                name: "PuntiAssegnati",
                table: "ManchesRisposte");

            migrationBuilder.DropColumn(
                name: "TempoImpiegatoMs",
                table: "ManchesRisposte");

            migrationBuilder.DropColumn(
                name: "DtAnnullamentoUtc",
                table: "ManchesDomande");

            migrationBuilder.DropColumn(
                name: "DurataSecondiOverride",
                table: "ManchesDomande");

            migrationBuilder.DropColumn(
                name: "IsAnnullata",
                table: "ManchesDomande");

            migrationBuilder.DropColumn(
                name: "MotivoAnnullamento",
                table: "ManchesDomande");

            migrationBuilder.DropColumn(
                name: "ScadenzaUtc",
                table: "ManchesDomande");

            migrationBuilder.DropColumn(
                name: "DtFineUtc",
                table: "Manches");

            migrationBuilder.DropColumn(
                name: "DtInizioUtc",
                table: "Manches");

            migrationBuilder.DropColumn(
                name: "MalusBase",
                table: "Manches");

            migrationBuilder.DropColumn(
                name: "Moltiplicatore",
                table: "Manches");

            migrationBuilder.DropColumn(
                name: "Ordine",
                table: "Manches");

            migrationBuilder.AlterColumn<char>(
                name: "Risposta",
                table: "ManchesRisposte",
                type: "TEXT",
                nullable: false,
                defaultValue: '\0',
                oldClrType: typeof(char),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ManchesRisposte_PlayerId",
                table: "ManchesRisposte",
                column: "PlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ManchesDomande_Domande_DomandaId",
                table: "ManchesDomande",
                column: "DomandaId",
                principalTable: "Domande",
                principalColumn: "Id");
        }
    }
}
