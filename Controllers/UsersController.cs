using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using projectPersonal.Data;
using projectPersonal.Hash;
using projectPersonal.Migrations;
using projectPersonal.Models;

namespace projectPersonal.Controllers
{

    public class UsersController : Controller
    {
        private readonly ApplicationDBcontext _db;
        private readonly SendMailService _sendMailService;
        public UsersController(ApplicationDBcontext db, SendMailService sendMailService)
        {
            this._db = db;
            _sendMailService = sendMailService;
        }
        public IActionResult Profile()
        {
            string username = HttpContext.Session.GetString("UserName");
            string email = HttpContext.Session.GetString("Email");

            ViewBag.Username = username;
            ViewBag.Email = email;

            var user = _db.users.FirstOrDefault(x => x.Username == username && x.Email == email);

            // Ensure the user model is not null
            if (user == null)
            {
                user = new User
                {
                    Username = username,
                    Email = email
                };
            }

            return View(user);
        }
        [HttpPost]
        public async Task<IActionResult> Profile(User obj, string UserName, IFormFile imageFile)
        {
            string username = HttpContext.Session.GetString("UserName");
            ViewBag.Username = username;
            string email = HttpContext.Session.GetString("Email");
            ViewBag.Email = email;

            var user = _db.users.FirstOrDefault(x => x.Username == username);
            if (user != null)
            {
                user.First = obj.First;
                user.lastName = obj.lastName;
                user.Phonenumber = obj.Phonenumber;
                user.Address = obj.Address;
                user.Gender = obj.Gender;
                _db.users.Update(user);
                await _db.SaveChangesAsync();

                // Trả về JSON với dữ liệu người dùng
                return Json(new { success = true, data = user });
            }
            return Json(new { success = false, message = "User not found" });
        }
        [HttpPost]
        public async Task<IActionResult> EditImg(User obj, string UserName, IFormFile imageFile)
        {
            string username = HttpContext.Session.GetString("UserName");
            ViewBag.Username = username;
            string email = HttpContext.Session.GetString("Email");
            ViewBag.Email = email;
            var user = _db.users.FirstOrDefault(x => x.Username == username);

            if (imageFile != null && imageFile.Length > 0)
            {
                var validImageTypes = new[] { "image/jpeg", "image/png", "image/gif" };
                if (!validImageTypes.Contains(imageFile.ContentType))
                {
                    return BadRequest("Chỉ chấp nhận các tệp hình ảnh (JPEG, PNG, GIF).");

                }
                var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.Image?.TrimStart('/'));
                if (System.IO.File.Exists(oldImagePath))
                {
                    // Xóa ảnh cũ
                    System.IO.File.Delete(oldImagePath);
                }
                var newImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", imageFile.FileName);

                // Tạo thư mục nếu không tồn tại
                var directory = Path.GetDirectoryName(newImagePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var stream = new FileStream(newImagePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                user.Image = "/images/" + imageFile.FileName;
                _db.users.Update(user);
                await _db.SaveChangesAsync();

                // Trả về đường dẫn ảnh mới
                return Json(new { newImagePath = user.Image });
            }

            return BadRequest("Không có file ảnh nào được tải lên.");
        }


        [HttpGet]
        public IActionResult ChangePassWord()
        {
            // TODO: Your code here
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ChangePassWord(OTPChangePassWord us)
        {
            var user = this._db.users.FirstOrDefault(x => x.Username == us.Username && x.Email == us.Email);
            if (user != null)
            {
                // Tạo OTP
                var otpCode = GenerateOTP();
                var otp = new OTP()
                {
                    Email = us.Email,
                    Code = otpCode,
                    ExpirationTime = DateTime.Now.AddMinutes(2)
                };
                this._db.oTPs.Add(otp);

                // Lưu thay đổi vào cơ sở dữ liệu
                var saveChangesResult = await this._db.SaveChangesAsync();

                if (saveChangesResult > 0) // Kiểm tra lưu thành công
                {
                    // Create send Mail
                    var mailContent = new MailContent
                    {
                        To = us.Email,
                        Subject = "Change Password",
                        Body = $"<h1>Welcome to our application!</h1><p>Your OTP code is: {otpCode}</p>"
                    };

                    var mailResult = await _sendMailService.SendMail(mailContent);

                    if (mailResult.StartsWith("Error"))
                    {
                        ViewBag.Error = mailResult;
                        return View(new OTPChangePassWord { Email = us.Email }); // Trả về view với đối tượng OTPChangePassWord
                    }
                    else
                    {
                        ViewBag.Message = "ChangePassword successful! Please check your email for the OTP code.";

                        // Lưu người dùng tạm vào TempData
                        TempData["User"] = JsonConvert.SerializeObject(us);
                        HttpContext.Session.SetString("OTP", JsonConvert.SerializeObject(otp));
                        HttpContext.Session.SetString("User", JsonConvert.SerializeObject(user));
                        return RedirectToAction("ConfirmChangePassWord", new { email = us.Email });
                    }
                }
                else
                {
                    ViewBag.Error = "An error occurred while saving data.";
                    return View(new OTPChangePassWord { Email = us.Email }); // Trả về view với đối tượng OTPChangePassWord
                }
            }
            else
            {
                ViewBag.Error = "User not found.";
                return View(new OTPChangePassWord()); // Trả về view với đối tượng OTPChangePassWord
            }
        }

        private string GenerateOTP()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        [HttpGet]
        public IActionResult ConfirmChangePassWord(string email)
        {
            var otpJson = HttpContext.Session.GetString("OTP");
            if (otpJson == null)
            {
                ModelState.AddModelError(string.Empty, "OTP not found.");
                return View();
            }

            var otp = JsonConvert.DeserializeObject<OTP>(otpJson);
            var model = new OTPChangePassWord
            {
                Email = email,
                ExpirationTime = otp.ExpirationTime
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmChangePassWord(OTPChangePassWord model)
        {
            var otpJson = HttpContext.Session.GetString("OTP");
            if (otpJson == null)
            {
                ModelState.AddModelError(string.Empty, "OTP not found.");
                return View(model);
            }

            var otp = JsonConvert.DeserializeObject<OTP>(otpJson);

            if (otp.Email != model.Email || otp.Code != model.OTPCode || otp.ExpirationTime < DateTime.Now)
            {
                ModelState.AddModelError("OTPCode", "Invalid or expired OTP.");
                return View(model);
            }

            if (model.NewPassword != model.ConfirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Passwords do not match.");
                return View(model);
            }

            // Update the user's password
            var userJson = HttpContext.Session.GetString("User");
            if (userJson == null)
            {
                ModelState.AddModelError(string.Empty, "User data not found!");
                return RedirectToAction("Profile", "Users");
            }

            var user = JsonConvert.DeserializeObject<User>(userJson);
            user.Password = MD5Hash.CalculateMD5Hash(model.NewPassword);

            _db.users.Update(user);
            await _db.SaveChangesAsync();
            ViewBag.Message = "ChangePassword confirmed! You can now login.";
            return RedirectToAction("Login", "Home");
        }
        [HttpGet]
        public async Task<IActionResult> ResendOTPChangePassWord(string email)
        {
            var oldOtp = await this._db.oTPs.Where(x => x.Email == email).ToListAsync();
            this._db.oTPs.RemoveRange(oldOtp);
            await this._db.SaveChangesAsync();
            var otpCode = GenerateOTP();
            var newOpt = new OTP()
            {
                Email = email,
                Code = otpCode,
                ExpirationTime = DateTime.Now.AddMinutes(2)
            };
            this._db.oTPs.Add(newOpt);
            await this._db.SaveChangesAsync();
            var mailContent = new MailContent
            {
                To = email,
                Subject = "New OTP Code Change PassWord",
                Body = $"<h1>Here is your new OTP code:</h1><p>{otpCode}</p>"
            };
            var result = await _sendMailService.SendMail(mailContent);

            if (result.StartsWith("Error"))
            {
                return Json(new { success = false, message = result });
            }
            HttpContext.Session.SetString("OTP", JsonConvert.SerializeObject(newOpt));

            return Json(new { success = true });

        }


    }
}