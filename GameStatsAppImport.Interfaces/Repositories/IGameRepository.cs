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
        IEnumerable<GameIGDBID> GetGameIGDBIDs(Expression<Func<GameIGDBID, bool>> predicate = null);
        void InsertGames(IEnumerable<Game> games);
        void SaveGames(IEnumerable<Game> games);
        void UpdateGameCoverImages(IEnumerable<Game> games);
    }
}






