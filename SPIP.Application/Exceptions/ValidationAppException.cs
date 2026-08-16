namespace SPIP.Application.Exceptions;

public class ValidationAppException : Exception
{
    public IEnumerable<string> Errors { get; }

    public ValidationAppException(IEnumerable<string> errors) : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }
}
