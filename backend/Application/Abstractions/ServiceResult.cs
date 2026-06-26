namespace Parlamento.Application.Abstractions;

public sealed class ServiceResult<T>
{
    private ServiceResult(bool isSuccess, T? value, int statusCode, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Value = value;
        StatusCode = statusCode;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }

    public T? Value { get; }

    public int StatusCode { get; }

    public string? ErrorMessage { get; }

    public static ServiceResult<T> Success(T value, int statusCode = 200)
    {
        return new ServiceResult<T>(true, value, statusCode, null);
    }

    public static ServiceResult<T> Failure(int statusCode, string errorMessage)
    {
        return new ServiceResult<T>(false, default, statusCode, errorMessage);
    }
}
