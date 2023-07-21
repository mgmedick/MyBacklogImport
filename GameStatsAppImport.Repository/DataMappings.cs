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
            For<Game>().PrimaryKey("ID").TableName("tbl_Game");
            For<Setting>().PrimaryKey("ID").TableName("tbl_Setting");
        }
    }
}



