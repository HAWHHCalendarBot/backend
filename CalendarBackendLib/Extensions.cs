﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalendarBackendLib
{
    public static class Extensions
    {
        public static async Task<T[]> WhenAll<T>(this IEnumerable<Task<T>> tasks)
        {
            return await Task.WhenAll(tasks);
        }

        public static Task WaitAll(this IEnumerable<Task> tasks)
        {
            return Task.WhenAll(tasks);
        }
    }
}
