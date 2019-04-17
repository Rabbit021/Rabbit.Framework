using System;
using System.Diagnostics;

namespace Rabbit.WebApiFramework.Core
{
    public sealed class OperationMonitor : IDisposable
    {
        private readonly Stopwatch watcher;
        private readonly string message;
        private readonly int gcCollectionCount;

        public OperationMonitor(string message)
        {
            Preparation();
            this.message = message;
            gcCollectionCount = GC.CollectionCount(0);
            watcher = Stopwatch.StartNew();
        }

        private static void Preparation()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        public void Dispose()
        {
            Console.WriteLine($"{message}:{watcher.Elapsed} GC={GC.CollectionCount(0) - gcCollectionCount}");
        }
    }
}