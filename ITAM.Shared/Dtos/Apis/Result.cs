namespace ITAM.Shared.Dtos.Apis;

public class Result<T>
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public T? Data { get; set; }
}
