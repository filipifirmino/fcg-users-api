using FCG.UsersAPI.Api.Middlewares;

namespace FCG.UsersAPI.Api.Extensions
{
    public static class GlobalExceptionHandlerExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionHandler>();
        }
    }
}
