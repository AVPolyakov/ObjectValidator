using System.Collections.Generic;

namespace ObjectValidator
{
    public class ValidationContext
    {
        public List<FailureData> Errors { get; } = new List<FailureData>();
        private readonly HashSet<string> set = new HashSet<string>();

        public bool Contains(string propertyName) => set.Contains(propertyName);

        public void Add(string propertyName, FailureData failureData)
        {
            Errors.Add(failureData);
            set.Add(propertyName);
        }

        public void Add(FailureData failureData) => Errors.Add(failureData);
    }
}