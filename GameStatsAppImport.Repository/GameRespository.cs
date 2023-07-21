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

namespace GameStatsAppImport.Repository
{
    public class GameRespository : BaseRepository, IGameRepository
    {
        public IEnumerable<Game> GetGames(Expression<Func<Game, bool>>  predicate)
        {
            using (IDatabase db = DBFactory.GetDatabase())
            {
                return db.Query<Game>().Where(predicate).ToList();
            }
        }               
    }
}

