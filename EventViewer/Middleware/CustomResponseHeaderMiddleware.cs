using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventViewer.Middleware
{
    public class CustomResponseHeaderMiddleware
    {
        private readonly RequestDelegate _next;

        public CustomResponseHeaderMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            
            context.Response.OnStarting(state =>
            {
                var httpContext = (HttpContext)state;
                httpContext.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000");
                httpContext.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                httpContext.Response.Headers.Add("X-Xss-Protection", "1; mode=block");
                httpContext.Response.Headers.Add("Content-Security-Policy",
                             "default-src 'none'; script-src 'self'; connect-src 'self'; img-src 'self' data:;font-src 'self' https://fonts.gstatic.com; style-src 'self' fonts.googleapis.com;base-uri 'self';form-action 'self'; report-uri /cspreport");
                //... and so on
                return Task.CompletedTask;
            }, context);

            await _next(context);
        }
    }
}
