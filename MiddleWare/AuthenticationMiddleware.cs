using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.CookiePolicy;
using projectPersonal.Data;
using projectPersonal.Models;

namespace projectPersonal.MiddleWare
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        public AuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task Invoke(HttpContext context, ApplicationDBcontext dBContext)
        {
            var path = context.Request.Path.Value;
          

            if (context.Session.TryGetValue("UserName", out _))
            {
                string username = context.Session.GetString("UserName");
                if (username != null)
                {
                    User getInfo = dBContext.users.FirstOrDefault(u => u.Username == username);
                    if (getInfo != null && getInfo.RoleID == 4 || getInfo.RoleID == 5 )
                    {
                        if (!context.Request.Path.Equals("/Home/AccessDenied"))
                        {
                            context.Response.Redirect("/Home/AccessDenied");
                            context.Session.Clear();
                            return;
                        }
                    }
                }
                await _next(context);
            }
            else
            {
                if (path.StartsWith("/Admin") || path.StartsWith("/User") || path.StartsWith("/HRM"))
                {
                    context.Response.Redirect("/Home/Login");
                    return;
                }
                await _next(context);
            }

        }
}
    
}