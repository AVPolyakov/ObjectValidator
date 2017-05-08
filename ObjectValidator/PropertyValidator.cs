using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation.Resources;

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

        public static IPropertyValidator<T, string> NotEmpty<T>(this IPropertyValidator<T, string> @this, Func<string> message = null)
        {
            @this.Command.Add(
                @this.PropertyName,
                () => {
                    if (string.IsNullOrWhiteSpace(@this.Value))
                    {
                        var errorTuple = ErrorTuple.Create(message, () => Messages.notempty_error);
                        return new ErrorInfo {
                            PropertyName = @this.PropertyName,
                            DisplayPropertyName = @this.DisplayName,
                            Code = errorTuple.Code,
                            Message = errorTuple.Message.ReplacePlaceholderWithValue(
                                CreateTuple("PropertyName", @this.DisplayName))
                        };
                    }
                    else
                        return null;
                });
            return @this;			
        }

        public static IPropertyValidator<T, TProperty> NotNull<T, TProperty>(this IPropertyValidator<T, TProperty> @this, Func<string> message = null)
        {
            @this.Command.Add(
                @this.PropertyName,
                () => {
                    object value = @this.Value;
                    if (value == null)
                    {
                        var errorTuple = ErrorTuple.Create(message, () => Messages.notnull_error);
                        return new ErrorInfo {
                            PropertyName = @this.PropertyName,
                            DisplayPropertyName = @this.DisplayName,
                            Code = errorTuple.Code,
                            Message = errorTuple.Message.ReplacePlaceholderWithValue(
                                CreateTuple("PropertyName", @this.DisplayName))
                        };
                    }
                    else
                        return null;
                });
            return @this;			
        }

        public static IPropertyValidator<T, TProperty> NotEqual<T, TProperty>(this IPropertyValidator<T, TProperty> @this, TProperty comparisonValue,
            Func<string> message = null)
        {
            @this.Command.Add(
                @this.PropertyName,
                () => {
                    if (Equals(@this.Value, comparisonValue))
                    {
                        var errorTuple = ErrorTuple.Create(message, () => Messages.notequal_error);
                        return new ErrorInfo {
                            PropertyName = @this.PropertyName,
                            DisplayPropertyName = @this.DisplayName,
                            Code = errorTuple.Code,
                            Message = errorTuple.Message.ReplacePlaceholderWithValue(
                                CreateTuple("PropertyName", @this.DisplayName),
                                CreateTuple("ComparisonValue", comparisonValue))
                        };
                    }
                    else
                        return null;
                });
            return @this;			
        }

        public static IPropertyValidator<T, string> Length<T>(this IPropertyValidator<T, string> @this, int minLength, int maxLength,
            Func<string> message = null)
        {
            @this.Command.Add(
                @this.PropertyName,
                () => {
                    var length = @this.Value?.Length ?? 0;
                    if (length < minLength || length > maxLength)
                    {
                        var errorTuple = ErrorTuple.Create(message, () => Messages.length_error);
                        return new ErrorInfo {
                            PropertyName = @this.PropertyName,
                            DisplayPropertyName = @this.DisplayName,
                            Code = errorTuple.Code,
                            Message = errorTuple.Message.ReplacePlaceholderWithValue(
                                CreateTuple("PropertyName", @this.DisplayName),
                                CreateTuple("MaxLength", maxLength),
                                CreateTuple("MinLength", minLength),
                                CreateTuple("TotalLength", length))
                        };
                    }
                    else
                        return null;
                });
            return @this;			
        }

        public static IPropertyValidator<T, TProperty> If<T, TProperty>(this IPropertyValidator<T, TProperty> @this,
            Func<IPropertyValidator<T, TProperty>, bool> predicate, Func<string> message,
            params Func<IPropertyValidator<T, TProperty>, object>[] formatArgs) 
            => @this.If(_ => Task.FromResult(predicate(_)), message, formatArgs);

        public static IPropertyValidator<T, TProperty> If<T, TProperty>(this IPropertyValidator<T, TProperty> @this,
            Func<IPropertyValidator<T, TProperty>, Task<bool>> predicate, Func<string> message,
            params Func<IPropertyValidator<T, TProperty>, object>[] formatArgs)
        {
            @this.Command.Add(
                @this.PropertyName,
                async () => {
                    if (await predicate(@this))
                    {
                        var errorTuple = ErrorTuple.Create(message);
                        return new ErrorInfo {
                            PropertyName = @this.PropertyName,
                            DisplayPropertyName = @this.DisplayName,
                            Code = errorTuple.Code,
                            Message = string.Format(
                                errorTuple.Message.ReplacePlaceholderWithValue(
                                    CreateTuple("PropertyName", @this.DisplayName)),
                                formatArgs.Select(func => func(@this)).ToArray())
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

            public static ErrorTuple Create(Func<string> message, Func<string> defaultMessage) 
                => Create(message ?? defaultMessage);

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