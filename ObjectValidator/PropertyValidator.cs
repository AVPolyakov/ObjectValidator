using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ObjectValidator
{
    public interface IPropertyValidator<out T, out TProperty>: IValidator<TProperty>
    {
        IValidator<T> Validator { get; }
        string DisplayName { get; }
        T Object { get; }
        string ShortPropertyName { get; }
        string PropertyName { get; }
    }

    public class PropertyValidator<T, TProperty> : IPropertyValidator<T, TProperty>
    {
        public IValidator<T> Validator { get; }
        private readonly Func<T, TProperty> valueFunc;
        private readonly Func<string> name;
        private readonly string displayName;

        public PropertyValidator(IValidator<T> validator, Func<T, TProperty> valueFunc, Func<string> name, string displayName)
        {
            Validator = validator;
            this.valueFunc = valueFunc;
            this.name = name;
            this.displayName = displayName;
        }

        public TProperty Value => valueFunc(Object);

        public string DisplayName => displayName ?? ShortPropertyName;

        public T Object => Validator.Value;

        public string ShortPropertyName => name();

        public string PropertyName => string.Join(".", new[] {Validator.PropertyPrefix, ShortPropertyName}.Where(_ => !string.IsNullOrEmpty(_)));

        public ValidationCommand Command => Validator.Command;

        public string PropertyPrefix => PropertyValidatorExtensions.Validator(this).PropertyPrefix;
    }

    public static class PropertyValidatorExtensions 
    {
        public static IValidator<TProperty> Validator<T, TProperty>(this IPropertyValidator<T, TProperty> @this)
            => new Validator<TProperty>(@this.Value, @this.Command, $"{@this.PropertyName}");

        public static IEnumerable<IValidator<TProperty>> Validators<T, TProperty>(this IPropertyValidator<T, IEnumerable<TProperty>> @this)
        {
            var enumerable = @this.Value;
            return enumerable == null
                ? Enumerable.Empty<IValidator<TProperty>>()
                : enumerable.Select((item, i) => new Validator<TProperty>(
                    item, @this.Command, $"{@this.PropertyName}[{i}]"));
        }

        public static IPropertyValidator<T, IEnumerable<TProperty>> ForEach<T, TProperty>(this IPropertyValidator<T, IEnumerable<TProperty>> @this,
            Action<IValidator<TProperty>> action)
        {
            foreach (var item in @this.Validators())
                action(item);
            return @this;
        }

        public static IPropertyValidator<T, TProperty> NotNull<T, TProperty>(this IPropertyValidator<T, TProperty> @this)
            => @this.Add(v => {
                object value = v.Value;
                return value == null
                    ? v.CreateFailureData("'{PropertyName}' must not be empty.")
                    : null;
            });

        public static IPropertyValidator<T, TProperty> NotEmpty<T, TProperty>(this IPropertyValidator<T, TProperty> @this)
            => @this.Add(v => {
                object value = v.Value;
                bool b;
                var s = value as string;
                if (s != null)
                    b = string.IsNullOrWhiteSpace(s);
                else
                {
                    var enumerable = value as IEnumerable;
                    if (enumerable != null)
                        b = !enumerable.Cast<object>().Any();
                    else
                        b = Equals(value, default(TProperty));
                }
                return b
                    ? v.CreateFailureData("'{PropertyName}' should not be empty.")
                    : null;
            });

        public static IPropertyValidator<T, TProperty> NotEqual<T, TProperty>(this IPropertyValidator<T, TProperty> @this, TProperty comparisonValue)
            => @this.Add(v => Equals(v.Value, comparisonValue)
                ? v.CreateFailureData("'{PropertyName}' should not be equal to '{ComparisonValue}'.",
                    text => text.ReplacePlaceholderWithValue(MessageFormatter.CreateTuple("ComparisonValue", comparisonValue)))
                : null);

        public static IPropertyValidator<T, string> Length<T>(this IPropertyValidator<T, string> @this, int minLength, int maxLength)
            => @this.Add(v => {
                var length = @this.Value?.Length ?? 0;
                return length < minLength || length > maxLength
                    ? v.CreateFailureData("'{PropertyName}' must be between {MinLength} and {MaxLength} characters. You entered {TotalLength} characters.",
                        text => text.ReplacePlaceholderWithValue(
                            MessageFormatter.CreateTuple("MaxLength", maxLength),
                            MessageFormatter.CreateTuple("MinLength", minLength),
                            MessageFormatter.CreateTuple("TotalLength", length)))
                    : null;
            });

        public static IPropertyValidator<T, TProperty> InclusiveBetween<T, TProperty>(this IPropertyValidator<T, TProperty> @this, TProperty from, TProperty to)
            where TProperty : IComparable<TProperty>, IComparable
            => @this.Add(v => {
                return from.CompareTo(@this.Value) > 0 || to.CompareTo(@this.Value) < 0
                    ? v.CreateFailureData("'{PropertyName}' must be between {From} and {To}. You entered {Value}.",
                        text => text.ReplacePlaceholderWithValue(
                            MessageFormatter.CreateTuple("From", from),
                            MessageFormatter.CreateTuple("To", to),
                            MessageFormatter.CreateTuple("Value", @this.Value)))
                    : null;
            });

        public static IPropertyValidator<T, TProperty> ExclusiveBetween<T, TProperty>(this IPropertyValidator<T, TProperty> @this, TProperty from, TProperty to)
            where TProperty : IComparable<TProperty>, IComparable
            => @this.Add(v => from.CompareTo(@this.Value) >= 0 || to.CompareTo(@this.Value) <= 0
                ? v.CreateFailureData("'{PropertyName}' must be between {From} and {To}. You entered {Value}.",
                    text => text.ReplacePlaceholderWithValue(
                        MessageFormatter.CreateTuple("From", from),
                        MessageFormatter.CreateTuple("To", to),
                        MessageFormatter.CreateTuple("Value", @this.Value)))
                : null);

        public static FailureData CreateFailureData<T, TProperty>(this IPropertyValidator<T, TProperty> @this, string message,
            Func<string, string> converter = null)
        {
            var text = message
                .ReplacePlaceholderWithValue(MessageFormatter.CreateTuple("PropertyName", @this.DisplayName));
            return new FailureData(
                errorMessage: converter != null ? converter(text) : text,
                propertyName: @this.PropertyName,
                propertyLocalizedName: @this.DisplayName);
        }

        public static IPropertyValidator<T, TProperty> Add<T, TProperty>(this IPropertyValidator<T, TProperty> @this,
            Func<IPropertyValidator<T, TProperty>, Task<FailureData>> func)
        {
            @this.Command.Add(@this.PropertyName, () => func(@this));
            return @this;			
        }

        public static IPropertyValidator<T, TProperty> Add<T, TProperty>(this IPropertyValidator<T, TProperty> @this,
            Func<IPropertyValidator<T, TProperty>, FailureData> func)
        {
            @this.Command.Add(@this.PropertyName, () => func(@this));
            return @this;
        }
    }
}