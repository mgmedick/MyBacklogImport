﻿using System;
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
using Microsoft.AspNetCore.Http;
using GameStatsAppImport.Repository;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.Extensions.Configuration;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace GameStatsAppImport.Service
{
    public class UserService : BaseService, IUserService
    {
        private readonly IUserRepository _userRepo = null;
        private readonly ISettingService _settingService = null;
        private readonly IConfiguration _config = null;
        private readonly ILogger _logger = null;

        public UserService(IUserRepository userRepo, ISettingService settingService, IConfiguration config, ILogger logger)
        {
            _userRepo = userRepo;
            _settingService = settingService;
            _config = config;
            _logger = logger;
        }

        public bool DeleteDemoUsers()
        {
            bool result = true;

            try
            {
                _logger.Information("Started DeleteDemoUsers");
                var emailDomain = _config.GetSection("SiteSettings").GetSection("EmailDomain").Value;
                var users = _userRepo.GetUsers(i => i.Email.EndsWith("demo@" + emailDomain) && !i.Deleted).ToList();

                foreach (var user in users)
                {
                    user.Active = false;
                    user.Deleted = true;
                    user.ModifiedBy = 1;
                    user.ModifiedDate = DateTime.UtcNow;
                }

                _userRepo.SaveUsers(users);
                _logger.Information("Completed DeleteDemoUsers");
            }
            catch (Exception ex)
            {
                result = false;
                _logger.Error(ex, "DeleteDemoUsers");
            }

            return result;
        }
    }
}
 

