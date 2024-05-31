using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using projectPersonal.Data;
using projectPersonal.Models;

namespace projectPersonal.Controllers
{

    public class HRMController : Controller
    {
        private readonly ApplicationDBcontext _db;

        public HRMController(ApplicationDBcontext db)
        {
            this._db = db;
        }
        public async Task<IActionResult> ProfileHRM()
        {
            var username = HttpContext.Session.GetString("UserName");
            ViewBag.UserName = username;
            var list = await this._db.users.Where(u => u.RoleID == 2).ToListAsync();
            return View(list);
        }

        public async Task<IActionResult> EditUsers(string id)
        {
            var us = await this._db.users.FirstAsync(x => x.Username == id);
            if (us == null)
            {
                return NotFound();
            }
            return View(us);
        }
        [HttpPost]
        public async Task<IActionResult> EditUsers(User us, string username)
        {
            var obj = await this._db.users.FirstOrDefaultAsync(x => x.Username == username);
            if (obj != null)
            {
                obj.Phonenumber = us.Phonenumber;
                obj.Address = us.Address;
                this._db.users.Update(obj);
                await this._db.SaveChangesAsync();
                return RedirectToAction("ProfileHRM", "Users");
            }
            return View();
        }



    }
}