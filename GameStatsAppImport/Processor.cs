﻿using System;
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
        private readonly IAuthService _authService;
        private readonly ISettingService _settingService;
        private readonly IConfiguration _config;
        private readonly ILogger _logger;
        
        public Processor(IGameService gameService, IAuthService authService, ISettingService settingService, IConfiguration config, ILogger logger)
        {
            _gameService = gameService;
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

                var currDateUtc = DateTime.UtcNow;
                GameLastImportDateUtc = _settingService.GetSetting("GameLastImportDate")?.Dte ?? DateTime.MinValue;
                IsFullLoad = GameLastImportDateUtc == DateTime.MinValue;
                
                BaseService.TwitchClientID = _config.GetSection("Auth").GetSection("Twitch").GetSection("ClientID").Value;
                BaseService.TwitchAccessToken = await _authService.GetTwitchAccessToken();           
                BaseService.BaseWebPath = _config.GetSection("AppSettings").GetSection("BaseWebPath").Value;
                BaseService.GameImageWebPath = _config.GetSection("AppSettings").GetSection("GameImageWebPath").Value;
                BaseService.ImageFileExt = _config.GetSection("AppSettings").GetSection("ImageFileExt").Value;
                BaseService.TempImportPath = _config.GetSection("AppSettings").GetSection("TempImportPath").Value;
                BaseService.MaxPageLimit = Convert.ToInt32(_config.GetSection("ApiSettings").GetSection("MaxPageLimit").Value);

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

            var currDateUtc = DateTime.UtcNow;
            _settingService.UpdateSetting("ImportLastRunDate", currDateUtc);
            _logger.Information("Completed RunProcesses");            
        }

        public DateTime GameLastImportDateUtc { get; set; }
        public bool IsFullLoad { get; set; }
    }
}

