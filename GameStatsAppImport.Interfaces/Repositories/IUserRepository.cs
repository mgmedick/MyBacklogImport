using System;
using System.Collections.Generic;
using GameStatsAppImport.Model.Data;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace GameStatsAppImport.Interfaces.Repositories
{
    public interface IUserRepository
    {
        IEnumerable<User> GetUsers(Expression<Func<User, bool>>  predicate);
        void SaveUsers(IEnumerable<User> users);
    }
}






