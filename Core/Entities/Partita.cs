using Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Core.Entities
{

    public class Partita
    {
        public int Id { get; set; }
        public DateTime DtCreazione { get; set; } = DateTime.UtcNow;
        public PartitaStato Stato { get; set; } = PartitaStato.Nuova;
        public string Titolo { get; set; } = string.Empty;

        // Navigazioni (serviranno con EF)
        public ICollection<Manche> Manches { get; set; } = new List<Manche>();
        public ICollection<PlayerPartita> PlayersPartita { get; set; } = new List<PlayerPartita>();
    }
}
