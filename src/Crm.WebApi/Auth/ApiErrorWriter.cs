namespace Crm.WebApi.Auth;

/// <summary>
/// Writes authentication/authorization failures in the same unified error format
/// produced by <see cref="Middleware.ExceptionHandlingMiddleware"/>.
/// </summary>
public static class ApiErrorWriter
{
    public static async Task WriteAsync(HttpContext context, int statusCode, string code, string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new
        {
            error = new
            {
                code,
                message,
                details = Array.Empty<object>()
            }
        });
    }
}
