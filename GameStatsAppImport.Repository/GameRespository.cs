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
    public class GameRespository : BaseRepository, IGameRepository
    {
        private readonly ILogger _logger;

        public GameRespository(ILogger logger)
        {
            _logger = logger;
        }

        public IEnumerable<Game> GetGames(Expression<Func<Game, bool>>  predicate)
        {
            using (IDatabase db = DBFactory.GetDatabase())
            {
                return db.Query<Game>().Where(predicate).ToList();
            }
        }

        public IEnumerable<GameIGDBID> GetGameIGDBIDs(Expression<Func<GameIGDBID, bool>> predicate = null)
        {
            using (IDatabase db = DBFactory.GetDatabase())
            {
                db.OneTimeCommandTimeout = 32767;
                return db.Query<GameIGDBID>().Where(predicate ?? (x => true)).ToList();
            }
        }               

        public void InsertGames(IEnumerable<Game> games)
        {
            _logger.Information("Started InsertGames");
            int batchCount = 0;
            var gamesList = games.ToList();

            using (IDatabase db = DBFactory.GetDatabase())
            {
                while (batchCount < gamesList.Count)
                {
                    var gamesBatch = gamesList.Skip(batchCount).Take(BaseRepository.MaxBulkRows).ToList();
                    var gameIGDBIDs = gamesBatch.Select(i => i.IGDBID).Distinct().ToList();

                    using (var tran = db.GetTransaction())
                    {
                        db.InsertBatch<Game>(gamesBatch);
                        tran.Complete();
                    }

                    _logger.Information("Saved games {@Count} / {@Total}", gamesBatch.Count, gamesList.Count);
                    batchCount += MaxBulkRows;
                }
            }
            _logger.Information("Completed InsertGames");
        }

        public void SaveGames(IEnumerable<Game> games)
        {
            _logger.Information("Started SaveGames");
            int count = 1;
            var gamesList = games.ToList();

            using (IDatabase db = DBFactory.GetDatabase())
            {
                foreach (var game in gamesList)
                {
                    _logger.Information("Saving GameID: {@GameID}, IGDBID: {@IGDBID}", game.ID, game.IGDBID);

                    using (var tran = db.GetTransaction())
                    {
                        try
                        {
                            if (game.ID != 0)
                            {
                                game.ModifiedDate = DateTime.UtcNow;
                            }

                            _logger.Information("Saving game");
                            db.Save<Game>(game);
                            db.Save<GameIGDBID>(new GameIGDBID { GameID = game.ID, IGDBID = game.IGDBID });

                            _logger.Information("Completed Saving GameID: {@GameID}, IGDBID: {@IGDBID}", game.ID, game.IGDBID);
                            tran.Complete();
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, "SaveGames GameID: {@GameID}, IGDBID: {@IGDBID}", game.ID, game.IGDBID);
                        }
                    }

                    _logger.Information("Saved games {@Count} / {@Total}", count, gamesList.Count);
                    count++;
                }
            }
            _logger.Information("Completed SaveGames");
        }
    }
}

