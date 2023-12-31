﻿using System;
using System.Threading.Tasks;

namespace GameStatsAppImport.Interfaces.Services
{
    public interface IGameService
    {
        Task<bool> ProcessGames(DateTime lastImportDateUtc, bool isFullLoad);
        Task<bool> RefreshCache(DateTime lastImportDateUtc);
        Task<bool> RefreshCacheDemo(DateTime lastImportDateUtc);
    }
}
