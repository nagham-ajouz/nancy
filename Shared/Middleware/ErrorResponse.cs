namespace Shared.Middleware;

// The JSON shape returned to the client on every error
public class ErrorResponse
{
    public int    StatusCode { get; set; }
    public string Message    { get; set; } = null!;
    public string Type       { get; set; } = null!;
}