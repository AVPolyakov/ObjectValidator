using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation.Resources;
using static ObjectValidator.MessageFormatter;

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
        IPropertyValidator<T, TProperty> Add(Func<IPropertyValidator<T, TProperty>, Task<ErrorInfo>> func);
        IPropertyValidator<T, TProperty> Add(Func<IPropertyValidator<T, TProperty>, ErrorInfo> func);
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

        public IPropertyValidator<T, TProperty> Add(Func<IPropertyValidator<T, TProperty>, Task<ErrorInfo>> func)
        {
            Command.Add(PropertyName, () => func(this));
            return this;			
        }

        public IPropertyValidator<T, TProperty> Add(Func<IPropertyValidator<T, TProperty>, ErrorInfo> func)
        {
            Command.Add(PropertyName, () => func(this));
            return this;
        }
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

        public static IPropertyValidator<T, string> NotEmpty<T>(this IPropertyValidator<T, string> @this, Func<string> message = null)
            => @this.Add(v => string.IsNullOrWhiteSpace(v.Value)
                ? v.CreateErrorInfo(Message(message, () => Messages.notempty_error))
                : null);

        public static IPropertyValidator<T, TProperty> NotNull<T, TProperty>(this IPropertyValidator<T, TProperty> @this, Func<string> message = null)
            => @this.Add(v => {
                object value = v.Value;
                return value == null
                    ? v.CreateErrorInfo(Message(message, () => Messages.notnull_error))
                    : null;
            });

        public static IPropertyValidator<T, TProperty> NotEqual<T, TProperty>(this IPropertyValidator<T, TProperty> @this, TProperty comparisonValue,
            Func<string> message = null)
            => @this.Add(v => Equals(v.Value, comparisonValue)
                ? v.CreateErrorInfo(Message(message, () => Messages.notequal_error),
                    text => text.ReplacePlaceholderWithValue(CreateTuple("ComparisonValue", comparisonValue)))
                : null);

        public static IPropertyValidator<T, string> Length<T>(this IPropertyValidator<T, string> @this, int minLength, int maxLength,
            Func<string> message = null)
            => @this.Add(v => {
                var length = @this.Value?.Length ?? 0;
                return length < minLength || length > maxLength
                    ? v.CreateErrorInfo(Message(message, () => Messages.length_error),
                        text => text.ReplacePlaceholderWithValue(
                            CreateTuple("MaxLength", maxLength),
                            CreateTuple("MinLength", minLength),
                            CreateTuple("TotalLength", length)))
                    : null;
            });

        public static ErrorInfo CreateErrorInfo<T, TProperty>(this IPropertyValidator<T, TProperty> @this, Func<string> message,
            Func<string, string> converter = null)
        {
            var text = message().ReplacePlaceholderWithValue(CreateTuple("PropertyName", @this.DisplayName));
            return new ErrorInfo {
                PropertyName = @this.PropertyName,
                DisplayPropertyName = @this.DisplayName,
                Code = ReflectionUtil.GetMemberInfo(message).Name,
                Message = converter != null ? converter(text) : text
            };
        }

        private static Func<string> Message(Func<string> message, Func<string> defaultMessage)
            => message ?? defaultMessage;
    }
}