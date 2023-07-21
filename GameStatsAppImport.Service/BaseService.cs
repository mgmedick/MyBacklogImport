using GameStatsAppImport.Interfaces.Repositories;
using GameStatsAppImport.Interfaces.Services;
using GameStatsAppImport.Model;
using GameStatsAppImport.Model.Data;
using System.Collections.Generic;
using System.Linq;

namespace GameStatsAppImport.Service
{
    public abstract class BaseService
    {
        public static string TwitchClientID { get; set; }
        public static string TwitchAccessToken { get; set; }
        public static int PullDelayMS { get; set; }
        public static int ErrorPullDelayMS { get; set; }        
        public static int MaxPageLimit { get; set; }   
        public static int MaxRetryCount { get; set; }
        public static long MaxMemorySizeBytes { get; set; }
        public static string TempImportPath { get; set; }     
        public static string BaseWebPath { get; set; }
        public static string GameImageWebPath { get; set; }
        public static string ImageFileExt { get; set; }
    }
}
