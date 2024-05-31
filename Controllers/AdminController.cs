using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using projectPersonal.Data;
using projectPersonal.Models;

namespace projectPersonal.Controllers
{


    public class AdminController : Controller
    {
        private readonly ApplicationDBcontext _db;
        public AdminController(ApplicationDBcontext db)
        {
            this._db = db;
        }
        public async Task<IActionResult> Index()
        {

            var List = await this._db.users.Where(u => u.RoleID == 1).ToListAsync();
            var username = HttpContext.Session.GetString("UserName");
            ViewBag.Users = username;
            return View(List);
        }
        public async Task<IActionResult> EditUser(string id)
        {
            var obj = this._db.users.FirstOrDefault(x => x.Username == id);
            if (obj == null)
            {
                return NotFound();
            }

            return View(obj);
        }
        [HttpPost]
        public async Task<IActionResult> EditUser(User us, string username)
        {
            var obj = await this._db.users.FirstOrDefaultAsync(x => x.Username == username);
            if (obj != null)
            {
                obj.Username = us.Username;
                obj.Phonenumber = us.Phonenumber;
                obj.Email = us.Email;
                obj.Address = us.Address;
                obj.RoleID = us.RoleID;
                obj.Password = us.Password;
                this._db.users.Update(obj);
                await this._db.SaveChangesAsync();
                return RedirectToAction("Index", "Admin");
            }

            return View();
        }
        public async Task<IActionResult> Delete(string id)
        {
            var obj = await this._db.users.FirstOrDefaultAsync(x => x.Username == id);
            if (obj != null)
            {
                obj.RoleID = 4;
                this._db.users.Update(obj);
                await this._db.SaveChangesAsync();
                return RedirectToAction("Index", "Admin");
            }

            return View();
        }
        public async Task<IActionResult> RetoreUser()
        {
            var obj = await this._db.users.Where(x => x.RoleID == 4).ToListAsync();
            if (obj == null)
            {
                return NotFound();
            }
            return View(obj);
        }
        [HttpPost]
        public async Task<IActionResult> RetoreUser(User us, string id)
        {
            var obj = await this._db.users.FirstOrDefaultAsync(x => x.Username == id);
            if (obj != null)
            {
                obj.RoleID = 1;

                await this._db.SaveChangesAsync();
                return RedirectToAction("Index", "Admin");
            }
            return View();
        }
        public async Task<IActionResult> ManagementHRM()
        {
            var obj = await this._db.users.Where(x => x.RoleID == 2).ToListAsync();
            return View(obj);
        }
        public async Task<IActionResult> EditManagementHRM(string id)
        {
            var obj = await this._db.users.FirstOrDefaultAsync(x => x.Username == id);
            if (obj == null)
            {
                return NotFound();
            }

            return View(obj);
        }
        [HttpPost]
        public async Task<IActionResult> EditManagementHRM(User us, string username)
        {
            var obj = await this._db.users.FirstOrDefaultAsync(x => x.Username == username);
            if (obj != null)
            {
                obj.Username = us.Username;
                obj.Phonenumber = us.Phonenumber;
                obj.Email = us.Email;
                obj.RoleID = us.RoleID;
                obj.Address = us.Address;
                obj.Password = us.Password;
                this._db.users.Update(obj);
                await this._db.SaveChangesAsync();
                return RedirectToAction("ManagementHRM", "Admin");

            }

            return View();
        }
        public async Task<IActionResult> DeleteHRM(string id)
        {
            var obj = await this._db.users.FirstOrDefaultAsync(x => x.Username == id);
            if (obj != null)
            {
                obj.RoleID = 5;
                await this._db.SaveChangesAsync();
                return RedirectToAction("ManagementHRM", "Admin");
            }

            return View();
        }
        public async Task<IActionResult> ResoreHRM()
        {
            var obj = await this._db.users.Where(x => x.RoleID == 5).ToListAsync();
            return View(obj);
        }
        [HttpPost]
        public async Task<IActionResult> ResoreHRM(User us, string id)
        {
            var obj = await this._db.users.FirstOrDefaultAsync(x => x.Username == id);
            if (obj != null)
            {
                obj.RoleID = 2;
                await this._db.SaveChangesAsync();
                return RedirectToAction("ManagementHRM", "Admin");
            }

            return View();
        }


    }
}