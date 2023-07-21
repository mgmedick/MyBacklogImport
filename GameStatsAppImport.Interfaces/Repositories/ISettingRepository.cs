using System;
using System.Collections.Generic;
using GameStatsAppImport.Model.Data;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace GameStatsAppImport.Interfaces.Repositories
{
    public interface ISettingRepository
    {
        Setting GetSetting(string name);
        IEnumerable<Setting> GetSettings(Expression<Func<Setting, bool>> predicate = null);
        void UpdateSetting(Setting setting);
    }
}





