using System;
using System.Threading.Tasks;

namespace GameStatsAppImport.Interfaces.Services
{
    public interface IGameService
    {
        Task<bool> ProcessGames(DateTime lastImportDateUtc);
    }
}
