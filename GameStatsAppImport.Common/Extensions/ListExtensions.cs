using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace GameStatsAppImport.Common
{
    public static class ListHelper
    {
        public static void ClearMemory<T>(this List<T> list)
        {
            int id = GC.GetGeneration(list);
            list.Clear();
            GC.Collect(id, GCCollectionMode.Forced);
        }
    }
}

