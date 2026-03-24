using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace BeClean.Api.ExceptionHandler
{
    public interface IExceptionHandler
    {
        Task HandleExceptionAsync(HttpContext context, Exception exception);
    }

    public static class ExceptionHandlerExtensions
    {
        public static IServiceCollection AddGlobalExceptionHandler<T>(this IServiceCollection services) where T : class, IExceptionHandler
        {
            services.AddScoped<IExceptionHandler, T>();
            return services;
        }
    }
}
