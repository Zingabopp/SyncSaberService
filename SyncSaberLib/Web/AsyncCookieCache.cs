using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncSaberLib.Web
{
    public class AsyncCookieCache
    {
        private readonly Func<string, Task<string>> _valueFactory;
        private readonly ConcurrentDictionary<string, Lazy<Task<string>>> _map;

        public AsyncCookieCache(Func<string, Task<string>> valueFactory)
        {
            if (valueFactory == null) throw new ArgumentNullException("valueFactory");
            _valueFactory = valueFactory;
            _map = new ConcurrentDictionary<string, Lazy<Task<string>>>();
        }

        public Task<string> this[string key]
        {
            get
            {
                if (key == null) throw new ArgumentNullException("key");
                return _map.GetOrAdd(key, toAdd =>
                    new Lazy<Task<string>>(() => _valueFactory(toAdd))).Value;
            }
        }
    }

    public class TestCache
    {
        public TestCache()
        {
            var cache = new AsyncCookieCache(GetCookieAsync);
        }

        public static async Task<string> GetCookieAsync(string page)
        {
            return await Task.Run(() => { return "cookie"; });
        }
    }
}
