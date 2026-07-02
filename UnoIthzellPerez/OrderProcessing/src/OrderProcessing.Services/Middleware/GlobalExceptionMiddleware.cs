using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using DomainValidationException = OrderProcessing.Core.Exceptions.ValidationException;
using DomainNotFoundException = OrderProcessing.Core.Exceptions.NotFoundException;
using DomainConcurrencyException = OrderProcessing.Core.Exceptions.ConcurrencyException;

namespace OrderProcessing.Services.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Method} {Path} failed", context.Request.Method, context.Request.Path);

            var (status, message) = ex switch
            {
                DomainNotFoundException => (HttpStatusCode.NotFound, ex.Message),
                DomainValidationException => (HttpStatusCode.BadRequest, ex.Message),
                ValidationException fv => (HttpStatusCode.BadRequest, string.Join("; ", fv.Errors.Select(e => e.ErrorMessage))),
                DomainConcurrencyException => (HttpStatusCode.Conflict, ex.Message),
                InvalidOperationException => (HttpStatusCode.BadRequest, ex.Message),
                _ => (HttpStatusCode.InternalServerError, "An error occurred. Please try again.")
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)status;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
        }
    }
}