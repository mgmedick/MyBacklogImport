using System;
using System.Collections.Generic;
using GameStatsAppImport.Model.Data;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace GameStatsAppImport.Interfaces.Repositories
{
    public interface IGameRepository
    {
        IEnumerable<Game> GetGames(Expression<Func<Game, bool>>  predicate);
    }
}






