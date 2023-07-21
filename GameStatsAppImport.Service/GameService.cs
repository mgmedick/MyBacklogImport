using System;
using GameStatsAppImport.Interfaces.Repositories;
using GameStatsAppImport.Interfaces.Services;
using GameStatsAppImport.Model.JSON;
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
        private readonly ILogger _logger = null;

        public GameService(IGameRepository gameRepo, ILogger logger)
        {
            _gameRepo = gameRepo;
            _logger = logger;
        }

        public async Task<bool> ProcessGames(DateTime lastImportDateUtc)
        {
            bool result = true;

            try
            {
                _logger.Information("Started ProcessGames: {@LastImportDateUtc}", lastImportDateUtc);
                var results = new List<Game>();
                var games = new List<Game>();
                var prevTotal = 0;

                do
                {
                    games = await GetGames(results.Count + prevTotal);
                    results.AddRange(games);
                    _logger.Information("Pulled games: {@New}, total games: {@Total}", games.Count, results.Count + prevTotal);
                    Thread.Sleep(TimeSpan.FromMilliseconds(BaseService.PullDelayMS));

                    var memorySize = GC.GetTotalMemory(false);
                    if (memorySize > BaseService.MaxMemorySizeBytes)
                    {
                        prevTotal += results.Count;
                        _logger.Information("Saving to clear memory, results: {@Count}, size: {@Size}", results.Count, memorySize);
                        //SaveGames(results);
                        results.ClearMemory();
                    }
                }
                while (games.Count == BaseService.MaxPageLimit && games.Min(i => new DateTime(i.created_at, DateTimeKind.Utc) > lastImportDateUtc));                
                _logger.Information("Completed ProcessGames");
            }
            catch (Exception ex)
            {
                result = false;
                _logger.Error(ex, "ProcessGames");
            }

            return result;
        }

        public async Task<List<Game>> GetGames(int offset, int retryCount = 0)
        {
            var data = new List<Game>();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("Client-ID", BaseService.TwitchClientID);
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + BaseService.TwitchAccessToken);

                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.igdb.com/v4/games");

                var parameters = new Dictionary<string, object> {
                    {"fields", "name,first_release_date,cover,created_at;"},
                    {"sort", "created_at desc;"},
                    {"limit", BaseService.MaxPageLimit},
                    {"offset", offset}
                };
                request.Content = new StringContent(JsonConvert.SerializeObject(parameters), Encoding.UTF8, "application/json");

                using (var response = await client.SendAsync(request))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var dataString = await response.Content.ReadAsStringAsync();
                        data = JsonConvert.DeserializeObject<List<Game>>(dataString);
                    }
                    else if (retryCount <= BaseService.MaxRetryCount)
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(BaseService.ErrorPullDelayMS));
                        retryCount++;
                        _logger.Information("Retrying pull games: {@New}, total games: {@Total}, retry: {@RetryCount}", BaseService.MaxPageLimit, offset, retryCount); 
                        data = await GetGames(offset, retryCount);                      
                    }
                }
            }

            return data;
        }                                  
    }
}
