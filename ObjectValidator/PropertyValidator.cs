using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Validators;

namespace ObjectValidator
{
    public interface IPropertyValidator<T, out TProperty>
    {
        IValidator<T> Validator { get; }
        Func<T, TProperty> Func { get; }
        string DisplayName { get; }
        T Object { get; }
        TProperty Value { get; }
        string ShortPropertyName { get; }
        string PropertyName { get; }
        ValidationCommand Command { get; }
    }

    public class PropertyValidator<T, TProperty> : IPropertyValidator<T, TProperty>
    {
        public IValidator<T> Validator { get; }
        public Func<T, TProperty> Func { get; }
        private readonly string displayName;

        public PropertyValidator(IValidator<T> validator, Func<T, TProperty> func, string displayName)
        {
            Validator = validator;
            Func = func;
            this.displayName = displayName;
        }

        public TProperty Value => Func(Object);

        public string DisplayName => displayName ?? ShortPropertyName;

        public T Object => Validator.Object;

        public string ShortPropertyName => ReflectionUtil.GetProperyInfo(Func).Name;

        public string PropertyName => $"{Validator.PropertyPrefix}{ShortPropertyName}";

        public ValidationCommand Command => Validator.Command;
    }

    public static class PropertyValidatorExtensions 
    {
        public static IValidator<TProperty> Validator<T, TProperty>(this IPropertyValidator<T, TProperty> @this)
            => new Validator<TProperty>(@this.Value, @this.Command, $"{@this.PropertyName}.");

        public static IEnumerable<IValidator<TProperty>> Validators<T, TProperty>(this IPropertyValidator<T, IEnumerable<TProperty>> @this)
        {
            var enumerable = @this.Value;
            return enumerable == null
                ? Enumerable.Empty<IValidator<TProperty>>()
                : enumerable.Select((item, i) => new Validator<TProperty>(
                    item, @this.Command, $"{@this.PropertyName}[{i}]."));
        }

        public static IPropertyValidator<T, TProperty> NotNull<T, TProperty>(this IPropertyValidator<T, TProperty> @this)
            => @this.Add(v => {
                object value = v.Value;
                return value == null
                    ? v.CreateFailureData(nameof(NotNullValidator))
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
                    ? v.CreateFailureData(nameof(NotEmptyValidator))
                    : null;
            });

        public static IPropertyValidator<T, TProperty> NotEqual<T, TProperty>(this IPropertyValidator<T, TProperty> @this, TProperty comparisonValue)
            => @this.Add(v => Equals(v.Value, comparisonValue)
                ? v.CreateFailureData(nameof(NotEqualValidator),
                    text => text.ReplacePlaceholderWithValue(MessageFormatter.CreateTuple("ComparisonValue", comparisonValue)))
                : null);

        public static IPropertyValidator<T, string> Length<T>(this IPropertyValidator<T, string> @this, int minLength, int maxLength)
            => @this.Add(v => {
                var length = @this.Value?.Length ?? 0;
                return length < minLength || length > maxLength
                    ? v.CreateFailureData(nameof(LengthValidator),
                        text => text.ReplacePlaceholderWithValue(
                            MessageFormatter.CreateTuple("MaxLength", maxLength),
                            MessageFormatter.CreateTuple("MinLength", minLength),
                            MessageFormatter.CreateTuple("TotalLength", length)))
                    : null;
            });

        public static IPropertyValidator<T, TProperty> If<T, TProperty>(this IPropertyValidator<T, TProperty> @this,
            Func<IPropertyValidator<T, TProperty>, bool> func, Func<string> message, params Func<IPropertyValidator<T, TProperty>, object>[] formatArgs) 
            => @this.If(value => Task.FromResult(func(value)), message, formatArgs);

        public static IPropertyValidator<T, TProperty> If<T, TProperty>(this IPropertyValidator<T, TProperty> @this,
            Func<IPropertyValidator<T, TProperty>, Task<bool>> func, Func<string> message, params Func<IPropertyValidator<T, TProperty>, object>[] formatArgs)
            => @this.Add(async v => await func(v)
                ? v.CreateFailureData(message, text => string.Format(text, formatArgs.Select(f => f(v)).ToArray()))
                : null);

        public static FailureData CreateFailureData<T, TProperty>(this IPropertyValidator<T, TProperty> @this, Func<string> message,
            Func<string, string> converter = null)
        {
            var text = message().ReplacePlaceholderWithValue(MessageFormatter.CreateTuple("PropertyName", @this.DisplayName));
            return new FailureData(
                errorMessage: converter != null ? converter(text) : text,
                errorCode: ReflectionUtil.GetMemberInfo(message).Name,
                propertyName: @this.PropertyName,
                propertyLocalizedName: @this.DisplayName);
        }

        public static FailureData CreateFailureData<T, TProperty>(this IPropertyValidator<T, TProperty> @this, string key,
            Func<string, string> converter = null)
        {
            var text = ValidatorOptions.LanguageManager.GetString(key)
                .ReplacePlaceholderWithValue(MessageFormatter.CreateTuple("PropertyName", @this.DisplayName));
            return new FailureData(
                errorMessage: converter != null ? converter(text) : text,
                errorCode: key,
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