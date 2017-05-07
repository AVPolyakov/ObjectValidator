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
        string LocalizedName { get; }
    }

    public class PropertyValidator<T, TProperty> : IPropertyValidator<T, TProperty>
    {
        public IValidator<T> Validator { get; }
        public Func<T, TProperty> Func { get; }
        public string LocalizedName { get; }

        public PropertyValidator(IValidator<T> validator, Func<T, TProperty> func, string localizedName)
        {
            Validator = validator;
            Func = func;
            LocalizedName = localizedName;
        }
    }

    public static class PropertyValidatorExtensions
    {
        public static string ShortPropertyName<T, TProperty>(this IPropertyValidator<T, TProperty> @this) 
            => ReflectionUtil.GetProperyInfo(@this.Func).Name;

        public static string PropertyName<T, TProperty>(this IPropertyValidator<T, TProperty> @this) 
            => $"{@this.Validator.PropertyPrefix}{@this.ShortPropertyName()}";

        public static TProperty Value<T, TProperty>(this IPropertyValidator<T, TProperty> @this) 
            => @this.Func(@this.Validator.Object);

        public static string LocalizedPropertyName<T, TProperty>(this IPropertyValidator<T, TProperty> @this) 
            => @this.LocalizedName ?? @this.ShortPropertyName();

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

        public static IPropertyValidator<T, string> NotEmpty<T>(this IPropertyValidator<T, string> @this)
        {
            @this.Validator.Command.Add(
                @this.PropertyName(),
                () => string.IsNullOrEmpty(@this.Value())
                    ? new ErrorInfo {
                        PropertyName = @this.PropertyName(),
                        Message = Messages.notempty_error.ReplacePlaceholderWithValue(
                            CreateTuple("PropertyName", @this.LocalizedPropertyName()))
                    }
                    : null
            );
            return @this;			
        }

        private static string ReplacePlaceholderWithValue(this string seed, params Tuple<string, object>[] tuples)
            => tuples.Aggregate(seed, (current, tuple) => current.Replace($"{{{tuple.Item1}}}", tuple.Item2?.ToString()));

        private static Tuple<string, object> CreateTuple(string key, object value) => Tuple.Create(key, value);
    }
}