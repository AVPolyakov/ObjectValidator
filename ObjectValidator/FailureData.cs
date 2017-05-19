namespace ObjectValidator
{
	public class FailureData
	{
	    public string ErrorMessage { get; }
	    public string ErrorCode { get; }
	    private readonly string propertyName;
	    private readonly string propertyLocalizedName;

	    public FailureData(string errorMessage, string errorCode, string propertyName = null, string propertyLocalizedName = null)
		{
		    this.propertyName = propertyName;
		    this.propertyLocalizedName = propertyLocalizedName;
		    ErrorMessage = errorMessage;
		    ErrorCode = errorCode;
		}

		public string GetPropertyName() => propertyName;

	    public string GetPropertyLocalizedName() => propertyLocalizedName;
	}
}
