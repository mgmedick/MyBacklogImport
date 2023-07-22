using System;
using System.Collections.Generic;
using System.Text;
using NPoco.FluentMappings;
using GameStatsAppImport.Model.Data;

namespace GameStatsAppImport.Repository
{
    public class DataMappings : Mappings
    {
        public DataMappings()
        {
            For<Game>().PrimaryKey("ID").TableName("tbl_Game").Columns(i =>
            {
                i.Column(g => g.IGDBID).Ignore();
            });
            For<GameIGDBID>().PrimaryKey("GameID", false).TableName("tbl_Game_IGDBID");
            For<Setting>().PrimaryKey("ID").TableName("tbl_Setting");
        }
    }
}



