using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityCore
{
    /// <summary>Retains one reference to an asset or preload group until disposed.</summary>
    public sealed class AssetLease<T> : IDisposable
    {
        private readonly T asset;
        private readonly Func<bool> valid;
        private Action release;

        internal AssetLease(T asset, Func<bool> valid, Action release)
        {
            this.asset = asset;
            this.valid = valid;
            this.release = release;
        }

        public T Asset => release != null && valid()
            ? asset
            : throw new ObjectDisposedException(nameof(AssetLease<T>));

        public void Dispose()
        {
            var callback = release;
            release = null;
            callback?.Invoke();
        }
    }

    /// <summary>Shared Resources cache. Use and dispose leases on the Unity main thread.</summary>
    public sealed class ResourceStore : IDisposable
    {
        private readonly Dictionary<(string Key, Type Result, bool Group), Entry> entries = new();
        private bool disposed;

        private sealed class Entry
        {
            public readonly TaskCompletionSource<object> Result = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public int References;
            public bool Closed;
        }

        public Task<AssetLease<T>> AcquireAsync<T>(string address) where T : UnityEngine.Object =>
            Acquire<T>(address, false, entry => StartSingleLoad<T>(address, entry));

        public Task<AssetLease<IList<T>>> PreloadAsync<T>(string label) where T : UnityEngine.Object =>
            Acquire<IList<T>>(label, true, entry => StartGroupLoad<T>(label, entry));

        private void StartSingleLoad<T>(string address, Entry entry) where T : UnityEngine.Object
        {
            var id = (address, typeof(T), false);
            var request = Resources.LoadAsync<T>(address);
            request.completed += _ =>
            {
                if (entry.Closed) return;
                if (request.asset is T typed && typed != null)
                {
                    entry.Result.TrySetResult(typed);
                }
                else
                {
                    entries.Remove(id);
                    entry.Result.TrySetException(new InvalidOperationException($"Could not load '{address}'."));
                    Close(entry);
                }
            };
        }

        private void StartGroupLoad<T>(string label, Entry entry) where T : UnityEngine.Object
        {
            var id = (label, typeof(IList<T>), true);
            var assets = Resources.LoadAll<T>(label);
            if (entry.Closed) return;
            if (assets != null && assets.Length > 0)
            {
                entry.Result.TrySetResult((IList<T>)assets);
            }
            else
            {
                entries.Remove(id);
                entry.Result.TrySetException(new InvalidOperationException($"Could not load '{label}'."));
                Close(entry);
            }
        }

        private async Task<AssetLease<T>> Acquire<T>(string key, bool group, Action<Entry> start)
        {
            if (disposed) throw new ObjectDisposedException(nameof(ResourceStore));
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("An address or label is required.", nameof(key));

            var id = (key, typeof(T), group);
            if (entries.TryGetValue(id, out var entry))
            {
                entry.References++;
            }
            else
            {
                entry = new Entry { References = 1 };
                entries.Add(id, entry);
                start(entry);
            }

            try
            {
                var asset = (T)await entry.Result.Task;
                if (entry.Closed) throw new ObjectDisposedException(nameof(ResourceStore));
                return new AssetLease<T>(asset, () => !entry.Closed, () => DropReference());
            }
            catch
            {
                DropReference();
                throw;
            }

            void DropReference()
            {
                if (entry.Closed || --entry.References > 0) return;
                if (entries.TryGetValue(id, out var current) && ReferenceEquals(entry, current))
                    entries.Remove(id);
                Close(entry);
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            var active = new List<Entry>(entries.Values);
            entries.Clear();
            foreach (var entry in active) Close(entry);
        }

        private static void Close(Entry entry)
        {
            entry.Closed = true;
            entry.Result.TrySetException(new ObjectDisposedException(nameof(ResourceStore)));
        }
    }
}
