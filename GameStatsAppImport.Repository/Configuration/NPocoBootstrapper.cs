using System;
using System.Collections.Generic;
using System.Text;
using NPoco;
using NPoco.FluentMappings;
using MySql.Data.MySqlClient;

namespace GameStatsAppImport.Repository.Configuration
{
    public static class NPocoBootstrapper
    {
        public static void Configure(string connectionString, int maxBulkRows)
        {
            var fluentConfig = FluentMappingConfiguration.Configure(new Repository.DataMappings());

            BaseRepository.DBFactory = DatabaseFactory.Config(i =>
            {
                i.UsingDatabase(() => new Database(connectionString, DatabaseType.MySQL, MySqlClientFactory.Instance, System.Data.IsolationLevel.ReadUncommitted));
                i.WithFluentConfig(fluentConfig);
            });
            BaseRepository.MaxBulkRows = maxBulkRows;
        }
    }
}
