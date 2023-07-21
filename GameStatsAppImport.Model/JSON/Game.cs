using System;
using System.Collections.Generic;
using System.Linq;

namespace GameStatsAppImport.Model.JSON
{
    public class Game
    {
        public int id { get; set; }
        public string name { get; set; }
        public long first_release_date { get; set; }
        public int cover { get; set; }
        public long created_at { get; set; }
    }
} 
