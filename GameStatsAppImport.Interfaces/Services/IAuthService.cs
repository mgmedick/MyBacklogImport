using System;
using GameStatsAppImport.Model;
using GameStatsAppImport.Model.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameStatsAppImport.Interfaces.Services
{
    public interface IAuthService
    {
        Task<string> GetTwitchAccessToken();
    }
}
