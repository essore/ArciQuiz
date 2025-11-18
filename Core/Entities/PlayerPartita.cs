using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Entities
{
    public class PlayerPartita
    {
        public int PlayerId { get; set; }
        public int PartitaId { get; set; }
        public Player? Player { get; set; }
        public Partita? Partita { get; set; }
    }
}
