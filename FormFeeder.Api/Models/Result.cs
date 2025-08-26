namespace FormFeeder.Api.Models;

public readonly record struct Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    public Exception? Exception { get; init; }

    private Result(bool isSuccess, T? value, string? error, Exception? exception = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        Exception = exception;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    
    public static Result<T> Failure(string error) => new(false, default, error);
    
    public static Result<T> Failure(string error, Exception exception) => new(false, default, error, exception);

    public static implicit operator Result<T>(T value) => Success(value);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure) =>
        IsSuccess && Value is not null ? onSuccess(Value) : onFailure(Error ?? "Unknown error");

    public async Task<TResult> MatchAsync<TResult>(Func<T, Task<TResult>> onSuccess, Func<string, Task<TResult>> onFailure) =>
        IsSuccess && Value is not null ? await onSuccess(Value) : await onFailure(Error ?? "Unknown error");
}

public static class Result
{
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    
    public static Result<T> Failure<T>(string error) => Result<T>.Failure(error);
    
    public static Result<T> Failure<T>(string error, Exception exception) => Result<T>.Failure(error, exception);
}