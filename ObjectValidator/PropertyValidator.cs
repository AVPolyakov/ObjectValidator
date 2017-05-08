using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Resources;

namespace ObjectValidator
{
    public interface IPropertyValidator<T, out TProperty>
    {
        IValidator<T> Validator { get; }
        Func<T, TProperty> Func { get; }
        string DisplayName { get; }
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

        public string DisplayName => displayName ?? this.ShortPropertyName();
    }

    public static class PropertyValidatorExtensions
    {
        public static string ShortPropertyName<T, TProperty>(this IPropertyValidator<T, TProperty> @this) 
            => ReflectionUtil.GetProperyInfo(@this.Func).Name;

        public static string PropertyName<T, TProperty>(this IPropertyValidator<T, TProperty> @this) 
            => $"{@this.Validator.PropertyPrefix}{@this.ShortPropertyName()}";

        public static TProperty Value<T, TProperty>(this IPropertyValidator<T, TProperty> @this) 
            => @this.Func(@this.Validator.Object);

        public static IValidator<TProperty> Validator<T, TProperty>(this IPropertyValidator<T, TProperty> @this)
            => new Validator<TProperty>(@this.Value(), @this.Validator.Command, $"{@this.PropertyName()}.");

        public static IEnumerable<IValidator<TProperty>> Validators<T, TProperty>(this IPropertyValidator<T, IEnumerable<TProperty>> @this)
        {
            var enumerable = @this.Value();
            return enumerable == null
                ? Enumerable.Empty<IValidator<TProperty>>()
                : enumerable.Select((item, i) => new Validator<TProperty>(
                    item, @this.Validator.Command, $"{@this.PropertyName()}[{i}]."));
        }

        public static IPropertyValidator<T, string> NotEmpty<T>(this IPropertyValidator<T, string> @this, Func<string> message = null)
        {
            @this.Validator.Command.Add(
                @this.PropertyName(),
                () => {
                    if (string.IsNullOrEmpty(@this.Value()))
                    {
                        var errorTuple = ErrorTuple.Create(message ?? (() => Messages.notempty_error));
                        return new ErrorInfo {
                            PropertyName = @this.PropertyName(),
                            DisplayPropertyName = @this.DisplayName,
                            Code = errorTuple.Code,
                            Message = errorTuple.Message.ReplacePlaceholderWithValue(
                                CreateTuple("PropertyName", @this.DisplayName)),
                        };
                    }
                    else
                        return null;
                });
            return @this;			
        }

        private static string ReplacePlaceholderWithValue(this string seed, params Tuple<string, object>[] tuples)
            => tuples.Aggregate(seed, (current, tuple) => current.Replace($"{{{tuple.Item1}}}", tuple.Item2?.ToString()));

        private static Tuple<string, object> CreateTuple(string key, object value) => Tuple.Create(key, value);

        private class ErrorTuple
        {
            public static ErrorTuple Create(Func<string> message) 
                => new ErrorTuple(ReflectionUtil.GetMemberInfo(message).Name, message());

            public string Code { get; }
            public string Message { get; }

            public ErrorTuple(string code, string message)
            {
                Code = code;
                Message = message;
            }
        }
    }
}