namespace BrowserApp.Server.DTOs.Responses;

/// <summary>
/// Standard error response format for the API.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Error message.
    /// </summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// Additional error details (optional).
    /// </summary>
    public string? Details { get; set; }

    public ErrorResponse()
    {
    }

    public ErrorResponse(string error)
    {
        Error = error;
    }

    public ErrorResponse(string error, string details)
    {
        Error = error;
        Details = details;
    }
}
