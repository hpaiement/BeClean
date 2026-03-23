using BeClean.Localization;
using BeClean.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace BeClean.Api.ExceptionHandler
{
    public sealed class EFExceptionHandler(
        IUnitOfWorkRegistry _unitOfWorkRegistry, 
        ILogger<EFExceptionHandler> _logger,
        LocaleStringService _localStringService) : IExceptionHandler
    {
        public async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            if (exception.InnerException != null)
                exception = exception.InnerException;
            _logger.LogError($"Exception occured during request handling: {exception}");

            await _unitOfWorkRegistry.RollbackAllAsync(); // Rollback any ongoing transactions. The Unit of work is passed by dependency injection

            SetResponseCode(context, exception);
            string message = GetErrorMessage(exception);
            
            await context.Response.WriteAsync(message);
        }

        private void SetResponseCode(HttpContext context, Exception exception)
        {
            if (exception is UnauthorizedAccessException)
            {
                context.Response.ContentType = "text/plain";
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            }
            else
            {
                context.Response.ContentType = "text/plain";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
        }

        private string GetErrorMessage(Exception exception)
        {
            if (exception is LocalizedUnauthorizedException luex)
            {
                return string.Format(_localStringService.GetLocaleString(luex.Message), luex.Args);
            }
            else if (exception is UnauthorizedAccessException)
            {
                return _localStringService.GetLocaleString("Unauthorized");
            }
            else if (exception is LocalizedUserException lex)
            {
                return string.Format(_localStringService.GetLocaleString(lex.Message), lex.Args);
            }
            else
            {
                return exception.Message.ToString();
            }
        }
    }
}
