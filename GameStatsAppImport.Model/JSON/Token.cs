using System;
using System.Collections.Generic;
using System.Linq;

namespace GameStatsAppImport.Model.JSON
{
    public class Token
    {
        public string access_token { get; set; }
        public double expires_in { get; set; }
        public string token_type { get; set; }
    }
} 
