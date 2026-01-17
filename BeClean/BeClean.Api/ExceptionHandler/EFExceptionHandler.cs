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
            context.Response.ContentType = "text/plain";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var message = exception.Message.ToString();
            if (exception is LocalizedUserException lex)
            {
                message = string.Format(_localStringService.GetLocaleString(lex.Message), lex.Args);
            }
            
            await context.Response.WriteAsync(message);
        }
    }
}
