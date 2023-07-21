using System.Runtime.Serialization;
using System.ComponentModel;

namespace GameStatsAppImport.Model
{
    public enum GameAccountType
    {
        Steam = 1,
        Xbox = 2
    } 

    public enum DefaultGameList
    {
        AllGames = 1,
        Backlog = 2,
        Playing = 3,
        Completed = 4
    } 
}
