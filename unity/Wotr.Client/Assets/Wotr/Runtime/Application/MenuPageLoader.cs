using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Wotr.Client
{
    internal sealed class MenuPage<T>
    {
        public List<T> Items = new List<T>();
        public string NextCursor = "";
    }

    // Keep API cursors behind the scroll view. Refresh only the portion already read.
    internal sealed class MenuPageLoader<T>
    {
        private readonly Func<string, CancellationToken, Task<MenuPage<T>>> fetch;
        private readonly Func<T, string> identity;
        public List<T> Items { get; private set; } = new List<T>();
        public string NextCursor { get; private set; } = "";

        public MenuPageLoader(Func<string, CancellationToken, Task<MenuPage<T>>> fetch, Func<T, string> identity)
        {
            this.fetch = fetch;
            this.identity = identity;
        }

        public async Task Refresh(CancellationToken token)
        {
            var target = Math.Max(1, Items.Count);
            var items = new List<T>();
            var cursor = "";
            do
            {
                var page = await fetch(cursor, token);
                items.AddRange(page.Items);
                if (page.NextCursor.Length > 0 && page.NextCursor == cursor)
                    throw new InvalidOperationException("Repeated menu cursor.");
                cursor = page.NextCursor;
            } while (cursor.Length > 0 && items.Count < target);
            token.ThrowIfCancellationRequested();
            Items = Unique(items);
            NextCursor = cursor;
        }

        public async Task More(CancellationToken token)
        {
            if (NextCursor.Length == 0) return;
            var page = await fetch(NextCursor, token);
            token.ThrowIfCancellationRequested();
            if (page.NextCursor.Length > 0 && page.NextCursor == NextCursor)
                throw new InvalidOperationException("Repeated menu cursor.");
            Items = Unique(Items.Concat(page.Items));
            NextCursor = page.NextCursor;
        }

        private List<T> Unique(IEnumerable<T> items)
        {
            var ids = new HashSet<string>();
            return items.Where(item => ids.Add(identity(item))).ToList();
        }
    }
}
