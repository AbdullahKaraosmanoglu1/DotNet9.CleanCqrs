using DotNet9.Api.Common;
using FluentValidation;
using System.Net;
using System.Text.Json;

namespace DotNet9.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException vex)
        {
            // 400 + validation detayları
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";
            var errors = vex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}").ToArray();
            var payload = ApiResponse<object>.Fail(errors);
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
        catch (InvalidOperationException ioex)
        {
            // örn: "Email already in use"
            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
            context.Response.ContentType = "application/json";
            var payload = ApiResponse<object>.Fail(ioex.Message);
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
        catch (KeyNotFoundException kex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            context.Response.ContentType = "application/json";
            var payload = ApiResponse<object>.Fail(kex.Message);
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            var payload = ApiResponse<object>.Fail("Internal server error");
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}
