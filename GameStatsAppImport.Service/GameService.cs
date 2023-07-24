using System;
using GameStatsAppImport.Interfaces.Repositories;
using GameStatsAppImport.Interfaces.Services;
using GameStatsAppImport.Model.JSON;
using GameStatsAppImport.Model.Data;
using GameStatsAppImport.Common;
using System.Collections.Generic;
using Serilog;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Linq;
using System.IO;

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
                //while (games.Count == BaseService.MaxPageLimit && (isFullLoad || games.Min(i => DateTimeOffset.FromUnixTimeSeconds(i.created_at).UtcDateTime) > lastImportDateUtc));                
                while (1 == 0);

                if (!isFullLoad)
                {
                    results.RemoveAll(i => DateTimeOffset.FromUnixTimeSeconds(i.created_at).UtcDateTime <= lastImportDateUtc);
                    //results.RemoveAll(i => new DateTime(i.created_at, DateTimeKind.Utc) <= lastImportDateUtc);
                }

                if (results.Any())
                {
                    SaveGames(results, isFullLoad);
                    var lastUpdateDate = results.Max(i => DateTimeOffset.FromUnixTimeSeconds(i.created_at).UtcDateTime);
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

                var parameters = new Dictionary<string, string> {
                    {"fields", "name,first_release_date,cover,created_at;"},
                    {"where", "category = 0;"},
                    {"sort", sort},
                    {"limit", BaseService.MaxPageLimit.ToString() + ";"},
                    {"offset", offset.ToString() + ";"}
                };
                var paramString = string.Join(" ", parameters.Select(i => i.Key + " " + i.Value).ToList());          
                request.Content = new StringContent(paramString, Encoding.UTF8, "application/json");

                using (var response = await client.SendAsync(request))
                {
                    try
                    {
                        response.EnsureSuccessStatusCode();
                        var dataString = await response.Content.ReadAsStringAsync();
                        data = JsonConvert.DeserializeObject<List<GameResponse>>(dataString);
                    }
                    catch (Exception ex)
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(BaseService.ErrorPullDelayMS));
                        retryCount++;
                        if (retryCount <= BaseService.MaxRetryCount)
                        {
                            _logger.Information("Retrying pull games: {@New}, total games: {@Total}, retry: {@RetryCount}", BaseService.MaxPageLimit, offset, retryCount);
                            data = await GetGameResponses(sort, offset, retryCount);
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }

            return data;
        }

        public void SaveGames(IEnumerable<GameResponse> gameResponses, bool isFullLoad)
        {
            _logger.Information("Started SaveGames: {@Count}, {@IsFullLoad}", gameResponses.Count(), isFullLoad);

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
                CoverIGDBID = i.cover,
                Name = i.name,
                ReleaseDate = DateTimeOffset.FromUnixTimeSeconds(i.first_release_date).UtcDateTime
            }).ToList();

            SetGameCoverUrls(games);

            if (isFullLoad)
            {
                _gameRepo.InsertGames(games);
            }
            else
            {
                _gameRepo.SaveGames(games);
            }

            ProcessGameCoverImages(games);
            _logger.Information("Completed SaveGames");
        }

        public void ProcessGameCoverImages(List<Game> games)
        {
            _logger.Information("Started ProcessGameCoverImages: {@Count}", games.Count);
            games = games.Where(i => !string.IsNullOrWhiteSpace(i.CoverImageUrl)).ToList();
            var tempGameCoverPaths = Task.Run(async () => await GetGameCoverImages(games)).Result;
            var gameCoverPaths = MoveGameCoverImages(tempGameCoverPaths);
            ClearTempFolder();
            SaveGameCoverImages(games, gameCoverPaths);
            _logger.Information("Completed ProcessGameCoverImages");
        }

        public async Task<Dictionary<int, string>> GetGameCoverImages(List<Game> games)
        {
            _logger.Information("Started GetGameCoverImages: {@Count}", games.Count);
            var tempGameCoverPaths = new Dictionary<int, string>();

            int count = 1;
            using (HttpClient client = new HttpClient())
            {
                foreach (var game in games)
                {
                    var fileName = string.Format("GameCover_{0}.{1}", game.IGDBID, ImageFileExt);
                    var filePath = Path.Combine("/" + BaseService.GameImageWebPath, fileName);
                    if (!File.Exists(filePath))
                    {
                        var tempFilePath = Path.Combine(TempImportPath, fileName);
                        try
                        {
                            using (var response = await client.GetAsync(game.CoverImageUrl))
                            {
                                response.EnsureSuccessStatusCode();
                                using (var fs = new FileStream(tempFilePath, FileMode.CreateNew))
                                {
                                    await response.Content.CopyToAsync(fs);
                                }
                            }
                            Thread.Sleep(TimeSpan.FromMilliseconds(BaseService.PullDelayMS));
                        }
                        catch (Exception ex)
                        {
                            Thread.Sleep(TimeSpan.FromMilliseconds(BaseService.ErrorPullDelayMS));
                            _logger.Information(ex, "GetGameCoverImages");
                            tempFilePath = null;
                        }

                        if (!string.IsNullOrWhiteSpace(tempFilePath) && !tempGameCoverPaths.ContainsKey(game.ID))
                        {
                            tempGameCoverPaths.Add(game.ID, tempFilePath);
                        }
                    }
                    else
                    {
                        game.CoverImagePath = filePath;
                    }

                    _logger.Information("Set gameImage {@Count} / {@Total}", count, games.Count);
                    count++;
                }
            }

            _logger.Information("Completed GetGameCoverImages");

            return tempGameCoverPaths;
        }

        public Dictionary<int, string> MoveGameCoverImages(Dictionary<int, string> tempGameCoverPaths)
        {
            var gameCoverPaths = new Dictionary<int, string>();

            foreach (var tempGameCoverPath in tempGameCoverPaths)
            {
                var fileName = Path.GetFileName(tempGameCoverPath.Value);
                var destFilePath = Path.Combine(BaseWebPath, GameImageWebPath, fileName);
                if (File.Exists(tempGameCoverPath.Value))
                {
                    File.Move(tempGameCoverPath.Value, destFilePath, true);
                    var gameCoverPath = Path.Combine("/" + GameImageWebPath, fileName);
                    gameCoverPaths.Add(tempGameCoverPath.Key, gameCoverPath);
                }
            }

            return gameCoverPaths;
        }

        public void ClearTempFolder()
        {
            var di = new DirectoryInfo(TempImportPath);
            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
        }       

        public void SaveGameCoverImages(List<Game> games, Dictionary<int, string> gameCoverPaths)
        {
            foreach (var game in games)
            {
                if (gameCoverPaths.ContainsKey(game.ID))
                {
                    game.CoverImagePath = gameCoverPaths[game.ID];
                }
            }

            games = games.Where(i => !string.IsNullOrWhiteSpace(i.CoverImagePath)).ToList();
            _gameRepo.UpdateGameCoverImages(games);
        }

        public void SetGameCoverUrls(List<Game> games)
        {
            foreach(var game in games)
            {
                var imageID = Task.Run(async () => await GetGameCoverImageID(game.CoverIGDBID)).Result;
                game.CoverImageUrl = string.Format("https://images.igdb.com/igdb/image/upload/t_cover_big/{0}.jpg", imageID);
            }
        }

        public async Task<string> GetGameCoverImageID(int coverID, int retryCount = 0)
        {
            var result = string.Empty;

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("Client-ID", BaseService.TwitchClientID);
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + BaseService.TwitchAccessToken);

                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.igdb.com/v4/covers");

                var parameters = new Dictionary<string, object> {
                    {"fields", "image_id;"},
                    {"where", "id = " + coverID + ";"}
                };
                var paramString = string.Join(" ", parameters.Select(i => i.Key + " " + i.Value).ToList());          
                request.Content = new StringContent(paramString, Encoding.UTF8, "application/json");

                using (var response = await client.SendAsync(request))
                {
                    try
                    {
                        response.EnsureSuccessStatusCode();
                        var dataString = await response.Content.ReadAsStringAsync();
                        var items = JArray.Parse(dataString);
                        result = items.Select(obj => (string)obj["image_id"]).FirstOrDefault();
                    }
                    catch (Exception ex)
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(BaseService.ErrorPullDelayMS));
                        retryCount++;
                        if (retryCount <= BaseService.MaxRetryCount)
                        {
                            _logger.Information("Retrying pull cover: {@New}, total games: {@Total}, retry: {@RetryCount}", coverID, retryCount);
                            result = await GetGameCoverImageID(coverID, retryCount);
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }

            return result;
        }        
    }
}
 

