using System;
using GameStatsAppImport.Interfaces.Repositories;
using GameStatsAppImport.Interfaces.Services;
using GameStatsAppImport.Model.Data;
using Serilog;

namespace GameStatsAppImport.Service
{
    public class SettingService : ISettingService
    {
        private readonly ISettingRepository _settingRepo = null;
        private readonly ILogger _logger = null;

        public SettingService(ISettingRepository settingRepo, ILogger logger)
        {
            _settingRepo = settingRepo;
            _logger = logger;
        }

        public Setting GetSetting(string name)
        {
            return _settingRepo.GetSetting(name);
        }

        public void UpdateSetting(string name, DateTime value)
        {
            var setting = _settingRepo.GetSetting(name);
            setting.Dte = value;
            _settingRepo.UpdateSetting(setting);
        }

        public void UpdateSetting(string name, string value)
        {
            var setting = _settingRepo.GetSetting(name);
            setting.Str = value;
            _settingRepo.UpdateSetting(setting);
        }

        public void UpdateSetting(string name, int value)
        {
            var setting = _settingRepo.GetSetting(name);
            setting.Num = value;
            _settingRepo.UpdateSetting(setting);
        }

        public void UpdateSetting(Setting setting)
        {
            _settingRepo.UpdateSetting(setting);
        }
    }
}
