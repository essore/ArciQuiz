using Core.Enums;
using System;
using System.Collections.Generic;

namespace Core.Entities
{
    public class Partita
    {
        public int Id { get; set; }
        public DateTime DtCreazione { get; set; } = DateTime.UtcNow;
        public PartitaStato Stato { get; set; } = PartitaStato.Nuova;
        public GamePhase Fase { get; set; } = GamePhase.Idle;
        public string Titolo { get; set; } = string.Empty;

        public int? CurrentMancheId { get; set; }
        public int? CurrentMancheDomandaId { get; set; }
        public DateTime? DtInizioUtc { get; set; }
        public DateTime? DtFineUtc { get; set; }

        // Navigazioni
        public ICollection<Manche> Manches { get; set; } = new List<Manche>();
        public ICollection<PlayerPartita> PlayersPartita { get; set; } = new List<PlayerPartita>();
        public ICollection<Player> Players { get; set; } = new List<Player>();
    }
}

