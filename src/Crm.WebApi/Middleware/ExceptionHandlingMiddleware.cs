using Crm.Application.Exceptions;
using FluentValidation;

namespace Crm.WebApi.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Validation failed",
                ex.Errors.Select(x => new { field = x.PropertyName, message = x.ErrorMessage }));
        }
        catch (NotFoundException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status404NotFound, ex.Code, ex.Message);
        }
        catch (ConflictException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status409Conflict, ex.Code, ex.Message);
        }
        catch (UnauthorizedException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status401Unauthorized, ex.Code, ex.Message);
        }
        catch (ForbiddenException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status403Forbidden, ex.Code, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status401Unauthorized, "UNAUTHORIZED", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled API exception");
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "Internal server error");
        }
    }

    private static async Task WriteErrorAsync(
        HttpContext context,
        int statusCode,
        string code,
        string message,
        object? details = null)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new
        {
            error = new
            {
                code,
                message,
                details = details ?? Array.Empty<object>()
            }
        });
    }
}
