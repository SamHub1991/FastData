using System;
using System.Collections.Generic;

namespace FastData.SyncTool.WinForms
{
    public static class ListExtensions
    {
        public static void Swap<T>(this List<T> list, int index1, int index2)
        {
            if (index1 < 0 || index2 < 0 || index1 >= list.Count || index2 >= list.Count)
                throw new ArgumentOutOfRangeException("Index out of range");

            var temp = list[index1];
            list[index1] = list[index2];
            list[index2] = temp;
        }
    }
}