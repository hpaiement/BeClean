using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace BeClean.Api.ExceptionHandler
{
    public class ExceptionMiddleware(RequestDelegate _next)
    {
        public async Task InvokeAsync(HttpContext httpContext, IExceptionHandler exceptionHandler)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                await exceptionHandler.HandleExceptionAsync(httpContext, ex);
            }
        }
    }

    /// <summary>
    /// Extensions class to help using ExceptionMiddleware with DI pattern
    /// </summary>
    public static class ExceptionMiddlewareExtensions
    {
        public static WebApplication UseGlobalExceptionHandler(this WebApplication app)
        {
            app.UseMiddleware<ExceptionMiddleware>();
            return app;
        }
    }
}
