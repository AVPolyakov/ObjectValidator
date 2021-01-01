namespace ObjectValidator
{
	public class FailureData
	{
	    public string ErrorMessage { get; }
	    private readonly string propertyName;
	    private readonly string propertyLocalizedName;

	    public FailureData(string errorMessage, string propertyName = null, string propertyLocalizedName = null)
		{
		    this.propertyName = propertyName;
		    this.propertyLocalizedName = propertyLocalizedName;
		    ErrorMessage = errorMessage;
		}

		public string GetPropertyName() => propertyName;

	    public string GetPropertyLocalizedName() => propertyLocalizedName;
	}
}
