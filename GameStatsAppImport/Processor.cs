using System;
using Microsoft.Extensions.Configuration;
using Serilog;
using GameStatsAppImport.Repository.Configuration;
using GameStatsAppImport.Interfaces.Services;
using GameStatsAppImport.Service;
using GameStatsAppImport.Model.Data;
using System.IO;
using System.Data.SqlTypes;
using System.Threading.Tasks;

namespace GameStatsAppImport
{
    public class Processor
    {
        private readonly IGameService _gameService;
        private readonly IUserService _userService;
        private readonly IAuthService _authService;
        private readonly ISettingService _settingService;
        private readonly IConfiguration _config;
        private readonly ILogger _logger;
        
        public Processor(IGameService gameService, IUserService userService, IAuthService authService, ISettingService settingService, IConfiguration config, ILogger logger)
        {
            _gameService = gameService;
            _userService = userService;
            _authService = authService;
            _settingService = settingService;
            _config = config;
            _logger = logger;
        }

        public async Task Run()
        {
            try
            {
                await Init();
                await RunProcesses();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Run");
            }
        }

        public async Task Init()
        {
            try
            {
                _logger.Information("Started Init");
                var connString = _config.GetSection("ConnectionStrings").GetSection("DBConnectionString").Value;
                var maxBulkRows = Convert.ToInt32(_config.GetSection("AppSettings").GetSection("MaxBulkRows").Value);
                NPocoBootstrapper.Configure(connString, maxBulkRows);

                var sqlMinDateTime = (DateTime)SqlDateTime.MinValue;
                var currDateUtc = DateTime.UtcNow;
                GameLastImportDateUtc = _settingService.GetSetting("GameLastImportDate")?.Dte ?? sqlMinDateTime;
                IsFullLoad = _config.GetValue<bool>("IsFullLoad");

                BaseService.SqlMinDateTime = sqlMinDateTime;
                BaseService.TwitchClientID = _config.GetSection("Auth").GetSection("Twitch").GetSection("ClientID").Value;
                BaseService.TwitchAccessToken = await _authService.GetTwitchAccessToken();           
                BaseService.BaseWebPath = _config.GetSection("AppSettings").GetSection("BaseWebPath").Value;
                BaseService.GameImageWebPath = _config.GetSection("AppSettings").GetSection("GameImageWebPath").Value;
                BaseService.ImageFileExt = _config.GetSection("AppSettings").GetSection("ImageFileExt").Value;
                BaseService.TempImportPath = _config.GetSection("AppSettings").GetSection("TempImportPath").Value;
                BaseService.MaxPageLimit = Convert.ToInt32(_config.GetSection("AppSettings").GetSection("MaxPageLimit").Value);
                BaseService.MaxMemorySizeBytes = Convert.ToInt32(_config.GetSection("AppSettings").GetSection("MaxMemorySizeBytes").Value);                
                BaseService.MaxRetryCount = Convert.ToInt32(_config.GetSection("AppSettings").GetSection("MaxRetryCount").Value);                                
                BaseService.PullDelayMS = Convert.ToInt32(_config.GetSection("AppSettings").GetSection("PullDelayMS").Value);
                BaseService.ErrorPullDelayMS = Convert.ToInt32(_config.GetSection("AppSettings").GetSection("ErrorPullDelayMS").Value);

                if (!Directory.Exists(BaseService.TempImportPath))
                {
                    Directory.CreateDirectory(BaseService.TempImportPath);
                }

                _logger.Information("Completed Init");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Init");
            }
        }


        public async Task RunProcesses()
        {
            bool result = true;
            _logger.Information("Started RunProcesses");

            result = await _gameService.ProcessGames(GameLastImportDateUtc, IsFullLoad);
            result = _userService.DeleteDemoUsers();

            var currDateUtc = DateTime.UtcNow;
            _settingService.UpdateSetting("ImportLastRunDate", currDateUtc);
            _logger.Information("Completed RunProcesses");            
        }

        public DateTime GameLastImportDateUtc { get; set; }
        public bool IsFullLoad { get; set; }
    }
}

