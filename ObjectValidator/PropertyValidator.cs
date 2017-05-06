using System;

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
        public static string PropertyName<T, TProperty>(this IPropertyValidator<T, TProperty> @this) 
            => $"{@this.Validator.PropertyPrefix}{ReflectionUtil.GetProperyInfo(@this.Func).Name}";

        public static TProperty Value<T, TProperty>(this IPropertyValidator<T, TProperty> @this) 
            => @this.Func(@this.Validator.Object);

        public static IPropertyValidator<T, string> NotEmpty<T>(this IPropertyValidator<T, string> @this)
        {
            @this.Validator.Command.Add(
                @this.PropertyName(),
                () => string.IsNullOrEmpty(@this.Value())
                    ? new ErrorInfo {
                        PropertyName = @this.PropertyName(),
                        Message = $"'{@this.PropertyName()}' should not be empty."
                    }
                    : null
            );
            return @this;			
        }
    }
}