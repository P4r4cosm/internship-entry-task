using System.Net;
using System.Text.Json;
using FluentValidation;
using TicTacToe.Application.Common.Exceptions;

namespace TicTacToe.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        object? response = null;

        switch (exception)
        {
            case ValidationException validationException:
                statusCode = HttpStatusCode.BadRequest;
                // Формируем ответ согласно RFC 7807 для ошибок валидации
                response = new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", 
                    status = (int)statusCode, errors = validationException.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g
                            .Select(e => e.ErrorMessage)) };
                break;
            case NotFoundException:
                statusCode = HttpStatusCode.NotFound;
                response = new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.4", 
                    title = "The specified resource was not found.", status = (int)statusCode, detail = exception.Message };
                break;
            case BadRequestException:
                statusCode = HttpStatusCode.BadRequest;
                response = new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", 
                    title = "Invalid request.", status = (int)statusCode, detail = exception.Message };
                break;
            default:
                response = new { type = "https://tools.ietf.org/html/rfc7231#section-6.6.1", 
                    title = "An internal server error has occurred.", status = (int)statusCode, detail = exception.Message };
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}