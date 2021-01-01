namespace ObjectValidator
{
	public class FailureData
	{
	    public string ErrorMessage { get; }
	    private readonly string _propertyName;
	    private readonly string _propertyLocalizedName;

	    public FailureData(string errorMessage, string propertyName = null, string propertyLocalizedName = null)
		{
		    _propertyName = propertyName;
		    _propertyLocalizedName = propertyLocalizedName;
		    ErrorMessage = errorMessage;
		}

		public string GetPropertyName() => _propertyName;

	    public string GetPropertyLocalizedName() => _propertyLocalizedName;
	}
}
