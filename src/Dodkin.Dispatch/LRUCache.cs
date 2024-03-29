﻿namespace Dodkin.Dispatch
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    sealed class LRUCache<TKey, TValue> : IDisposable
        where TKey : notnull
        where TValue : notnull, IDisposable
    {
        private readonly ConcurrentDictionary<TKey, TValue> cache = new();
        private readonly Queue<TKey> accessOrder = new();
        private readonly object syncRoot = new();
        private readonly int maxCapacity;

        public LRUCache(int maxCapacity)
        {
            this.maxCapacity = maxCapacity;
        }

        public void Dispose()
        {
            foreach (var pair in this.cache)
            {
                pair.Value.Dispose();
            }
            this.cache.Clear();
        }

        public TValue GetOrAdd<TArg>(TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)
        {
            return this.cache.GetOrAdd(key, (k, arg) =>
            {
                TValue value = valueFactory(k, arg);
                lock (this.syncRoot)
                {
                    this.accessOrder.Enqueue(k);
                    while (this.accessOrder.Count > this.maxCapacity)
                    {
                        TKey oldestKey = this.accessOrder.Dequeue();
                        if (this.cache.TryRemove(oldestKey, out var oldestValue))
                        {
                            oldestValue.Dispose();
                        }
                    }
                }
                return value;
            }, factoryArgument);
        }
    }
}
