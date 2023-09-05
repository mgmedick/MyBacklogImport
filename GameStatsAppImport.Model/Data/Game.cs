using System;
using System.Collections.Generic;
using System.Linq;

namespace GameStatsAppImport.Model.Data
{
    public class Game
    {
        public int ID { get; set; }
        public int IGDBID { get; set; }
        public int CoverIGDBID { get; set; }
        public string Name { get; set; }
        public int GameCategoryID { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string CoverImageUrl { get; set; }
        public string CoverImagePath { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
} 
