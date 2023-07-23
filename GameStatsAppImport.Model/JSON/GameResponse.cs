using System;
using System.Collections.Generic;
using System.Linq;
using GameStatsAppImport.Model.Data;
using System.Text.Json.Serialization;

namespace GameStatsAppImport.Model.JSON
{
    public class GameResponse
    {
        public int id { get; set; }
        public string name { get; set; }
        public long first_release_date { get; set; }
        public int cover { get; set; }
        public long created_at { get; set; }
    }  
} 
