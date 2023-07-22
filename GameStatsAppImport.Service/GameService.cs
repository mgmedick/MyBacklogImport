﻿using System;
using GameStatsAppImport.Interfaces.Repositories;
using GameStatsAppImport.Interfaces.Services;
using GameStatsAppImport.Model.JSON;
using GameStatsAppImport.Model.Data;
using GameStatsAppImport.Common;
using System.Collections.Generic;
using Serilog;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Linq;

namespace GameStatsAppImport.Service
{
    public class GameService : BaseService, IGameService
    {
        private readonly IGameRepository _gameRepo = null;
        private readonly ISettingService _settingService = null;
        private readonly ILogger _logger = null;

        public GameService(IGameRepository gameRepo, ISettingService settingService, ILogger logger)
        {
            _gameRepo = gameRepo;
            _settingService = settingService;
            _logger = logger;
        }

        public async Task<bool> ProcessGames(DateTime lastImportDateUtc, bool isFullLoad)
        {
            bool result = true;

            try
            {
                _logger.Information("Started ProcessGames: {@LastImportDateUtc}, {@isFullLoad}", lastImportDateUtc, isFullLoad);
                var sort = isFullLoad ? "created_at;" : "created_at desc;";
                var results = new List<GameResponse>();
                var games = new List<GameResponse>();
                var prevTotal = 0;

                do
                {
                    games = await GetGameResponses(sort, results.Count + prevTotal);
                    Thread.Sleep(TimeSpan.FromMilliseconds(BaseService.PullDelayMS));
                    results.AddRange(games);
                    _logger.Information("Pulled games: {@New}, total games: {@Total}", games.Count, results.Count + prevTotal);

                    var memorySize = GC.GetTotalMemory(false);
                    if (memorySize > BaseService.MaxMemorySizeBytes)
                    {
                        prevTotal += results.Count;
                        _logger.Information("Saving to clear memory, results: {@Count}, size: {@Size}", results.Count, memorySize);
                        SaveGames(results, isFullLoad);
                        results.ClearMemory();
                    }
                }
                while (games.Count == BaseService.MaxPageLimit && games.Min(i => new DateTime(i.created_at, DateTimeKind.Utc) > lastImportDateUtc));                
                
                if (!isFullLoad)
                {
                    results.RemoveAll(i => new DateTime(i.created_at, DateTimeKind.Utc) <= lastImportDateUtc);
                }

                if (results.Any())
                {
                    SaveGames(results, isFullLoad);
                    var lastUpdateDate = results.Max(i => new DateTime(i.created_at, DateTimeKind.Utc));
                    _settingService.UpdateSetting("GameLastImportDate", lastUpdateDate);
                    results.ClearMemory();
                }
                
                _logger.Information("Completed ProcessGames");
            }
            catch (Exception ex)
            {
                result = false;
                _logger.Error(ex, "ProcessGames");
            }

            return result;
        }

        public async Task<List<GameResponse>> GetGameResponses(string sort, int offset, int retryCount = 0)
        {
            var data = new List<GameResponse>();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("Client-ID", BaseService.TwitchClientID);
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + BaseService.TwitchAccessToken);

                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.igdb.com/v4/games");

                var parameters = new Dictionary<string, object> {
                    {"fields", "name,first_release_date,cover,created_at;"},
                    {"sort", sort},
                    {"limit", BaseService.MaxPageLimit},
                    {"offset", offset}
                };
                request.Content = new StringContent(JsonConvert.SerializeObject(parameters), Encoding.UTF8, "application/json");

                using (var response = await client.SendAsync(request))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var dataString = await response.Content.ReadAsStringAsync();
                        data = JsonConvert.DeserializeObject<List<GameResponse>>(dataString);
                    }
                    else if (retryCount <= BaseService.MaxRetryCount)
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(BaseService.ErrorPullDelayMS));
                        retryCount++;
                        _logger.Information("Retrying pull games: {@New}, total games: {@Total}, retry: {@RetryCount}", BaseService.MaxPageLimit, offset, retryCount); 
                        data = await GetGameResponses(sort, offset, retryCount);                      
                    }
                }
            }

            return data;
        }

        public async Task<string> GetGameCoverUrl(int coverID, int retryCount = 0)
        {
            var data = string.Empty;

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("Client-ID", BaseService.TwitchClientID);
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + BaseService.TwitchAccessToken);

                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.igdb.com/v4/covers");

                var parameters = new Dictionary<string, object> {
                    {"fields", "url;"},
                    {"where", "id = " + coverID + ";"}
                };
                request.Content = new StringContent(JsonConvert.SerializeObject(parameters), Encoding.UTF8, "application/json");

                using (var response = await client.SendAsync(request))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var dataString = await response.Content.ReadAsStringAsync();
                        data = JsonConvert.DeserializeObject<string>(dataString);
                    }
                    else if (retryCount <= BaseService.MaxRetryCount)
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(BaseService.ErrorPullDelayMS));
                        retryCount++;
                        _logger.Information("Retrying pull cover: {@New}, total games: {@Total}, retry: {@RetryCount}", coverID, retryCount); 
                        data = await GetGameCoverUrl(coverID, retryCount);                      
                    }
                }
            }

            return data;
        }        

        public void SaveGames(IEnumerable<GameResponse> gameResponses, bool isFullLoad)
        {
            gameResponses = gameResponses.GroupBy(g => new { g.id })
                         .Select(i => i.First())
                         .ToList();

            gameResponses = gameResponses.OrderBy(i => i.created_at).ToList();
            var gameIDs = gameResponses.Select(i => i.id).ToList();
            var gameIGDBIDs = _gameRepo.GetGameIGDBIDs();
            gameIGDBIDs = gameIGDBIDs.Join(gameIDs, o => o.IGDBID, id => id, (o, id) => o).ToList();

            var games = gameResponses.Select(i => new Game()
            {
                ID = gameIGDBIDs.Where(g => g.IGDBID == i.id).Select(g => g.GameID).FirstOrDefault(),
                IGDBID = i.id,
                Name = i.name,
                ReleaseDate = new DateTime(i.first_release_date, DateTimeKind.Utc),
            }).ToList();

            if (isFullLoad)
            {
                _gameRepo.InsertGames(games);
            }
            else
            {
                _gameRepo.SaveGames(games);
            }
        }
    }
}
 

