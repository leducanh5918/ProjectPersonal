using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using projectPersonal.Data;
using projectPersonal.Models;
using System.Threading.Tasks;

namespace projectPersonal.MiddleWare
{
    public class SecondMidleWare
    {
        private readonly RequestDelegate _next;
        public SecondMidleWare(RequestDelegate next)
        {
            this._next = next;
        }
        public async Task Invoke(HttpContext context, ApplicationDBcontext dBContext)
        {
            var path = context.Request.Path.Value;
            if (path.Equals("/Home/Logout", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }// Get Path URL
            string username = context.Session.GetString("UserName");
            string email = context.Session.GetString("Email");
            var getinfo = dBContext.users.FirstOrDefault(u => u.Username == username || u.Email == email);

            if (getinfo != null)
            {
                System.Console.WriteLine(getinfo.RoleID);
                bool redirect = false;
                string redirectUrl = "/Home/AccessDenied";

                if (path.StartsWith("/User") && getinfo.RoleID != 1)
                {
                    redirect = true;
                }
                else if (path.StartsWith("/Admin") && getinfo.RoleID != 3)
                {
                    redirect = true;
                }
                else if (path.StartsWith("/HRM") && getinfo.RoleID != 2)
                {
                    redirect = true;
                }

                if (getinfo.RoleID == 1 && !path.StartsWith("/User"))
                {
                    redirect = true;
                    redirectUrl = "/Users/Profile";
                }
                else if (getinfo.RoleID == 2 && !path.StartsWith("/HRM"))
                {
                    redirect = true;
                    redirectUrl = "/HRM/ProfileHRM";
                }
                else if (getinfo.RoleID == 3 && !path.StartsWith("/Admin"))
                {
                    redirect = true;
                    redirectUrl = "/Admin/Index";
                }


                if (redirect)
                {
                    context.Response.Redirect(redirectUrl);
                    return;
                }
            }



            await _next(context);
        }
    }
}
