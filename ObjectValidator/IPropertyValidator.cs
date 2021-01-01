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
}