using System;
using GameStatsAppImport.Model;
using GameStatsAppImport.Model.Data;
using System.Collections.Generic;

namespace GameStatsAppImport.Interfaces.Services
{
    public interface ISettingService
    {
        Setting GetSetting(string name);
        void UpdateSetting(string name, string value);
        void UpdateSetting(string name, DateTime value);
        void UpdateSetting(string name, int value);
        void UpdateSetting(Setting setting); 
    }
}
