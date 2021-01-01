using System.Collections.Generic;

namespace ObjectValidator
{
    public class ValidationContext
    {
        public List<FailureData> Errors { get; } = new();
        private readonly HashSet<string> _set = new();

        public bool Contains(string propertyName) => _set.Contains(propertyName);

        public void Add(string propertyName, FailureData failureData)
        {
            Errors.Add(failureData);
            _set.Add(propertyName);
        }

        public void Add(FailureData failureData) => Errors.Add(failureData);
    }
}