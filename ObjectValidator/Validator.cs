using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ObjectValidator
{
    public interface IValidator<out T>
    {
        T Value { get; }
        ValidationCommand Command { get; }
        string PropertyPrefix { get; }
    }

    public class Validator<T> : IValidator<T>
    {
        public ValidationCommand Command { get; }
        public T Value { get; }
        public string PropertyPrefix { get; }

        public Validator(T @object, ValidationCommand command, string propertyPrefix = "")
        {
            Value = @object;
            Command = command;
            PropertyPrefix = propertyPrefix;
        }
    }

    public static class ValidatorExtensions
    {
        public static Task<List<FailureData>> Validate<T>(this IValidator<T> @this) => @this.Command.Validate();

        public static IValidator<T> Validator<T>(this T @object) => new Validator<T>(@object, new ValidationCommand());

        public static IPropertyValidator<T, TProperty> For<T, TProperty>(this IValidator<T> @this, Func<T, TProperty> func,
            string displayName = null)
            => new PropertyValidator<T, TProperty>(@this, func, () => ReflectionUtil.GetProperyInfo(func).Name, displayName);

        public static IPropertyValidator<T, T> For<T>(this IValidator<T> @this,
            string displayName = null)
            => new PropertyValidator<T, T>(@this, _ => _, () => "", displayName);
    }
}