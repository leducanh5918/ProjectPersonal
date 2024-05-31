using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace projectPersonal.MiddleWare
{
    public class FirstMidlleWare
    {
        private readonly RequestDelegate _next;
        public FirstMidlleWare(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Items.Add("DataFirst", context.Request.Path);
            Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");
            await _next(context);
        }

    }
}