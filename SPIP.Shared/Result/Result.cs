namespace SPIP.Shared.Result;

public class Result<T>
{
    public bool Succeeded { get; private set; }
    public T? Data { get; private set; }
    public string? Error { get; private set; }
    public IEnumerable<string>? Errors { get; private set; }

    private Result(bool succeeded, T? data, string? error, IEnumerable<string>? errors)
    {
        Succeeded = succeeded;
        Data = data;
        Error = error;
        Errors = errors;
    }

    public static Result<T> Success(T data) => new(true, data, null, null);
    public static Result<T> Failure(string error) => new(false, default, error, new[] { error });
    public static Result<T> Failure(IEnumerable<string> errors) => new(false, default, errors.FirstOrDefault(), errors);
}
