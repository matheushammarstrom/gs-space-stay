using SpaceStay.Core.Common;

namespace SpaceStay.Api.Middleware;

// Tratamento global de erros: traduz as exceções de domínio para o status HTTP correto
// (corpo em Problem Details). Erros inesperados viram 500 sem vazar a stack trace.
public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var (status, title) = ex switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, ex.Message),
                ConflictException => (StatusCodes.Status409Conflict, ex.Message),
                DomainValidationException => (StatusCodes.Status400BadRequest, ex.Message),
                AuthenticationException => (StatusCodes.Status401Unauthorized, ex.Message),
                ForbiddenException => (StatusCodes.Status403Forbidden, ex.Message),
                _ => (StatusCodes.Status500InternalServerError, "Erro interno do servidor.")
            };

            if (status == StatusCodes.Status500InternalServerError)
                logger.LogError(ex, "Erro não tratado");

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new
            {
                title,
                status,
                detail = status == StatusCodes.Status500InternalServerError ? null : ex.Message
            });
        }
    }
}
