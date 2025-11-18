using Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrasctructure.Data
{
    public static class ArciQuizDbInitializer
    {
        public static void Seed(ArciQuizDbContext db)
        {
            if (db.Partite.Any()) return;

            var partita = new Partita
            {
                Titolo = "Quiz di prova"
            };

            db.Partite.Add(partita);
            db.SaveChanges();
        }
    }

}
