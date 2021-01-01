using System;
using System.Linq;

namespace ObjectValidator
{
    public class PropertyValidator<T, TProperty> : IPropertyValidator<T, TProperty>
    {
        public IValidator<T> Validator { get; }
        private readonly Func<T, TProperty> _valueFunc;
        private readonly Func<string> _name;
        private readonly string _displayName;

        public PropertyValidator(IValidator<T> validator, Func<T, TProperty> valueFunc, Func<string> name, string displayName)
        {
            Validator = validator;
            _valueFunc = valueFunc;
            _name = name;
            _displayName = displayName;
        }

        public TProperty Value => _valueFunc(Object);

        public string DisplayName => _displayName ?? ShortPropertyName;

        public T Object => Validator.Value;

        public string ShortPropertyName => _name();

        public string PropertyName => string.Join(".", new[] {Validator.PropertyPrefix, ShortPropertyName}.Where(_ => !string.IsNullOrEmpty(_)));

        public ValidationCommand Command => Validator.Command;

        public string PropertyPrefix => PropertyValidatorExtensions.Validator(this).PropertyPrefix;
    }
}