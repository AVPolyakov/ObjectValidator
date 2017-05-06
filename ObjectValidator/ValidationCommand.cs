using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ObjectValidator
{
    public class ValidationCommand
    {
        private readonly List<Item> items = new List<Item>();

        public void Add(string propertyName, Func<ErrorInfo> func) => Add(propertyName, () => Task.FromResult(func()));

        public void Add(string propertyName, Func<Task<ErrorInfo>> func)
        {
            items.Add(new Item(propertyName, func));
        }

        public async Task<List<ErrorInfo>> Validate()
        {
            var result = new List<ErrorInfo>();
            var set = new HashSet<string>();
            foreach (var item in items)
            {
                if (!set.Contains(item.PropertyName))
                {
                    var errorInfo = await item.Func();
                    if (errorInfo != null)
                    {
                        result.Add(errorInfo);
                        set.Add(item.PropertyName);
                    }
                }
            }
            return result;
        }

        private class Item
        {
            public string PropertyName { get; }
            public Func<Task<ErrorInfo>> Func { get; }

            public Item(string propertyName, Func<Task<ErrorInfo>> func)
            {
                PropertyName = propertyName;
                Func = func;
            }
        }
    }
}
