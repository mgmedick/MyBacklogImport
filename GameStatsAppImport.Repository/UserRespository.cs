using System;
using System.Collections.Generic;
using NPoco;
using NPoco.Extensions;
using System.Linq;
using GameStatsAppImport.Model;
using GameStatsAppImport.Model.Data;
using GameStatsAppImport.Interfaces.Repositories;
using System.Linq.Expressions;
using System.Collections;
using Serilog;

namespace GameStatsAppImport.Repository
{
    public class UserRespository : BaseRepository, IUserRepository
    {
        private readonly ILogger _logger;

        public UserRespository(ILogger logger)
        {
            _logger = logger;
        }

        public IEnumerable<User> GetUsers(Expression<Func<User, bool>> predicate)
        {
            using (IDatabase db = DBFactory.GetDatabase())
            {
                return db.Query<User>().Where(predicate).ToList();
            }
        }

        public void SaveUsers(IEnumerable<User> users)
        {
            _logger.Information("Started SaveUsers");

            using (IDatabase db = DBFactory.GetDatabase())
            {
                using (var tran = db.GetTransaction())
                {
                    foreach (var user in users)
                    {
                        db.Save<User>(user);
                    }

                    tran.Complete();
                }
            }

            _logger.Information("Completed SaveUsers");
        }
    }
}




