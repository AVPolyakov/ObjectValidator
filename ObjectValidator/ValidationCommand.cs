using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ObjectValidator
{
    public class ValidationCommand
    {
        private readonly List<Func<ValidationContext, Task>> funcs = new List<Func<ValidationContext, Task>>();

        public void Add(Func<ValidationContext, Task> func) => funcs.Add(func);

        public void Add(Action<ValidationContext> action) => funcs.Add(context => {
            action(context);
            return Task.CompletedTask;
        });

        public void Add(FailureData failureData) => Add(context => context.Add(failureData));

        public void Add(string propertyName, Func<FailureData> func) => Add(propertyName, () => Task.FromResult(func()));

        public void Add(string propertyName, Func<Task<FailureData>> func)
        {
            Add(async context => {
                if (!context.Contains(propertyName))
                {
                    var failureData = await func();
                    if (failureData != null)
                        context.Add(propertyName, failureData);
                }
            });
        }

        public async Task<List<FailureData>> Validate()
        {
            var context = new ValidationContext();
            foreach (var func in funcs)
                await func(context);
            return context.Errors;
        }
    }
}
