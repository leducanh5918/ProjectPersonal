using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using projectPersonal.Data;
using projectPersonal.Hash;
using projectPersonal.Models;

namespace projectPersonal.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDBcontext _db;
        private readonly SendMailService _sendMailService;

        public HomeController(ApplicationDBcontext db, SendMailService sendMailService)
        {
            _db = db;
            _sendMailService = sendMailService;
        }

        public async Task<IActionResult> LoginGoogle()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleResponse")
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }
        public async Task<IActionResult> GoogleResponse(User us)
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (result.Succeeded)
            {
                var claims = result.Principal.Identities.FirstOrDefault().Claims.ToList();
                var userName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
                var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

                if (userName != null && email != null)
                {
                    // Save values to session
                    HttpContext.Session.SetString("UserName", userName);
                    HttpContext.Session.SetString("Email", email);
                    // Save to database
                    var user = new User
                    {
                        Username = userName,
                        Email = email,
                        Password = "",
                        RoleID = 1
                    };

                    // Check if the user already exists in the database
                    var existingUser = _db.users.FirstOrDefault(u => u.Email == email);
                    if (existingUser == null)
                    {
                        _db.users.Add(user);
                        await _db.SaveChangesAsync();
                    }

                    // Redirect to a secure page or user profile
                    return RedirectToAction("Profile", "Users");
                }
            }

            // Handle failed authentication
            return RedirectToAction("Login");
        }

        public IActionResult Login(string rememberedUserName, string rememberedEmail)
        {
            if (!string.IsNullOrEmpty(rememberedUserName) || !string.IsNullOrEmpty(rememberedEmail))
            {
                // Điền UserName vào trường nhập liệu trên giao diện đăng nhập
                ViewBag.RememberedUserName = rememberedUserName;
                ViewBag.RememberedEmail = rememberedEmail;
            }

            return View();
        }

        [HttpPost]
        public IActionResult Login(User obj, bool rememberMe)
        {
            User user = _db.users.FirstOrDefault(u => u.Username == obj.Username || u.Email == obj.Username);
            if (user != null && user.Password == MD5Hash.CalculateMD5Hash(obj.Password))
            {
                // Authentication logic here
                if (user.RoleID == 1)
                {
                    HttpContext.Session.SetString("UserName", user.Username);
                    HttpContext.Session.SetString("Email", user.Email);
                    if (rememberMe)
                    {
                        // Create a cookie to remember the user
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = true, // This makes the cookie persistent
                            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1) // Expires in 30 days
                        };
                        var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email)
                    // Add other claims if needed
                };
                        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var principal = new ClaimsPrincipal(identity);

                        HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
                    }
                    if (obj.Username.Contains("@"))
                    {
                        ViewBag.RememberedEmail = user.Email;
                        ViewBag.RememberedUserName = null;
                        HttpContext.Session.SetString("RememberedEmail", user.Email);

                    }
                    else
                    {
                        ViewBag.RememberedUserName = user.Username;
                        ViewBag.RememberedEmail = null;
                        HttpContext.Session.SetString("RememberedUserName", user.Username);

                    }
                    return RedirectToAction("Profile", "Users");
                }
                else if (user.RoleID == 2)
                {
                    HttpContext.Session.SetString("UserName", user.Username);
                    return RedirectToAction("ProfileHRM", "HRM");
                }
                else if (user.RoleID == 4)
                {
                    return RedirectToAction("AccessDenied", "Home");
                }
                else if (user.RoleID == 5)
                {
                    ModelState.AddModelError("AccessDenied", "Home");
                    return View();
                }
                else
                {
                    HttpContext.Session.SetString("UserName", user.Username);
                    HttpContext.Session.SetString("Email", user.Email);
                    return RedirectToAction("Index", "Admin");
                }
            }
            ModelState.AddModelError("Username", "UserName or PassWord error");

            return View();
        }
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(User us)
        {
            us.Password = MD5Hash.CalculateMD5Hash(us.Password);
            us.registrationDate = DateTime.Now;
            if (us.RoleID == 1 || us.RoleID == null)
            {
                us.RoleID = 1; // Mặc định chọn User nếu RoleID không được thiết lập hoặc đã được thiết lập thành User
            }
            else
            {
                us.RoleID = 2; // Chọn HRM nếu RoleID đã được thiết lập thành HRM
            }
            // Tạo OTP
            var otpCode = GenerateOTP();
            var otp = new OTP
            {
                Email = us.Email,
                Code = otpCode,
                ExpirationTime = DateTime.Now.AddMinutes(10) // OTP hết hạn sau 10 phút
            };

            // Lưu OTP vào cơ sở dữ liệu hoặc bộ nhớ tạm
            _db.oTPs.Add(otp);
            await _db.SaveChangesAsync();

            // Gửi OTP qua email
            var mailContent = new MailContent
            {
                To = us.Email,
                Subject = "Email Confirmation",
                Body = $"<h1>Welcome to our application!</h1><p>Your OTP code is: {otpCode}</p>"
            };

            var result = await _sendMailService.SendMail(mailContent);

            if (result.StartsWith("Error"))
            {
                ViewBag.Error = result;
                return View(us);
            }
            else
            {
                ViewBag.Message = "Registration successful! Please check your email for the OTP code.";
                // Lưu người dùng tạm vào TempData
                TempData["User"] = JsonConvert.SerializeObject(us);
                return RedirectToAction("ConfirmOTP", new { email = us.Email });
            }
        }

        private string GenerateOTP()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString(); // Tạo OTP 6 chữ số
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmOTP(string email)
        {
            var otp = await _db.oTPs.FirstOrDefaultAsync(o => o.Email == email);

            if (otp == null)
            {
                ModelState.AddModelError(string.Empty, "OTP not found.");
                return View();
            }

            var model = new ConfirmOTPViewModel
            {
                Email = email,
                ExpirationTime = otp.ExpirationTime
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmOTP(ConfirmOTPViewModel model)
        {
            var otp = await _db.oTPs.FirstOrDefaultAsync(o => o.Email == model.Email && o.Code == model.OTPCode);

            if (otp == null || otp.ExpirationTime < DateTime.Now)
            {
                ModelState.AddModelError(string.Empty, "Invalid or expired OTP.");
                return View(model);
            }

            // Xóa OTP sau khi xác nhận thành công
            _db.oTPs.Remove(otp);
            await _db.SaveChangesAsync();

            // Lấy người dùng tạm từ TempData
            var userJson = TempData["User"] as string;
            if (userJson == null)
            {
                ModelState.AddModelError(string.Empty, "User data not found. Please register again.");
                return RedirectToAction("Register");
            }

            var us = JsonConvert.DeserializeObject<User>(userJson);
            _db.users.Add(us);
            await _db.SaveChangesAsync();

            ViewBag.Message = "Registration confirmed! You can now login.";
            return RedirectToAction("Login");
        }
        [HttpGet]
        public async Task<IActionResult> ResendOTP(string email)
        {
            // Xóa tất cả các mã OTP cũ
            var oldOtps = await _db.oTPs.Where(o => o.Email == email).ToListAsync();
            _db.oTPs.RemoveRange(oldOtps);
            await _db.SaveChangesAsync();

            // Generate new OTP
            var otpCode = GenerateOTP();
            var newOtp = new OTP
            {
                Email = email,
                Code = otpCode,
                ExpirationTime = DateTime.Now.AddMinutes(2) // New expiration time
            };

            // Lưu OTP mới vào cơ sở dữ liệu
            _db.oTPs.Add(newOtp);
            await _db.SaveChangesAsync();

            // Send new OTP via email
            var mailContent = new MailContent
            {
                To = email,
                Subject = "New OTP Code",
                Body = $"<h1>Here is your new OTP code:</h1><p>{otpCode}</p>"
            };
            var result = await _sendMailService.SendMail(mailContent);

            if (result.StartsWith("Error"))
            {
                return Json(new { success = false, message = result });
            }

            return Json(new { success = true });
        }
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(User us)
        {
            var user = await _db.users.FirstOrDefaultAsync(u => u.Email == us.Email);
            if (user == null)
            {
                // User not found
                ModelState.AddModelError("Email", "Email not found");
                return View("ForgotPassword");
            }
            var otpCode = GenerateOTP();
            var otp = new OTP
            {
                Email = us.Email,
                Code = otpCode,
                ExpirationTime = DateTime.UtcNow.AddMinutes(2)
            };


            // Send email with reset password link
            var mailContent = new MailContent
            {
                To = user.Email,
                Subject = "Reset Password",
                Body = $"<h1>Welcome to our application Forget Password!</h1><p>Your OTP code is: {otpCode}</p>"
            };
            HttpContext.Session.SetString("OTPCode", JsonConvert.SerializeObject(otp));
            HttpContext.Session.SetString("User", JsonConvert.SerializeObject(user));


            var result = await _sendMailService.SendMail(mailContent);

            // Redirect to a confirmation page
            return RedirectToAction("ForgotPasswordConfirmation", new { Email = user.Email });
        }
        [HttpGet]
        public async Task<IActionResult> ForgotPasswordConfirmation(string email)
        {
            var otpJson = HttpContext.Session.GetString("OTPCode");
            if (otpJson == null)
            {
                ModelState.AddModelError(string.Empty, "OTP not found.");
                return View();
            }

            var otp = JsonConvert.DeserializeObject<OTP>(otpJson);
            var model = new OTPChangePassWord
            {
                Email = email,
                ExpirationTime = otp.ExpirationTime // Sử dụng thuộc tính ExpirationTime của đối tượng otp
            };

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> ForgotPasswordConfirmation(OTPChangePassWord model)
        {
            // Xác thực OTP từ cơ sở dữ liệu
            var otp = await _db.oTPs.FirstOrDefaultAsync(o => o.Email == model.Email && o.Code == model.OTPCode && o.ExpirationTime > DateTime.UtcNow);
            if (otp == null)
            {
                ModelState.AddModelError("OTPCode", "OTP không hợp lệ hoặc đã hết hạn.");
                return View(model);
            }

            var user = await _db.users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Người dùng không tồn tại.");
                return View(model);
            }

            var resetToken = GenerateResetToken();
            var otp1 = new OTP
            {
                Email = user.Email,
                Code = resetToken,
                ExpirationTime = DateTime.UtcNow.AddMinutes(2)
            };

            var resetPasswordUrl = Url.Action("ResetPassword", "Home", new { token = resetToken }, Request.Scheme);
            var mailContent = new MailContent
            {
                To = user.Email,
                Subject = "New Password",
                Body = $"Click the link to reset your password: <a href=\"{resetPasswordUrl}\">Reset Password</a>"
            };

            var result = await _sendMailService.SendMail(mailContent);
            if (result.StartsWith("Error"))
            {
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi gửi email.");
                return View(model);
            }

            user.ResetPasswordToken = resetToken;
            user.ResetPasswordTokenExpiration = DateTime.UtcNow.AddHours(1); // Token hết hạn sau 1 giờ
            _db.users.Update(user);
            await _db.SaveChangesAsync();

            // Xóa mã OTP sau khi sử dụng
            _db.oTPs.Remove(otp);
            await _db.SaveChangesAsync();

            // Xóa mã OTP khỏi session sau khi sử dụng
            HttpContext.Session.Remove("OTPCode");

            ViewBag.Message = "An email with a reset password link has been sent to your email address.";
            return RedirectToAction("Login", "Home");
        }
        private string GenerateResetToken()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString(); // Generate a random token
        }
        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            // Validate token
            var user = _db.users.FirstOrDefault(u => u.ResetPasswordToken == token && u.ResetPasswordTokenExpiration > DateTime.UtcNow);
            if (user == null)
            {
                // Invalid or expired token
                return RedirectToAction("Login", "Home");
            }

            // Pass token to the view
            ViewBag.Token = token;
            HttpContext.Session.SetString("resetPasswordToken", token); // Lưu token vào Session
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string token, User us)
        {
            var token1 = HttpContext.Session.GetString("token");
            // Validate token
            var tokenFromSession = HttpContext.Session.GetString("resetPasswordToken");

            // Validate token
            var user = await _db.users.FirstOrDefaultAsync(u => u.ResetPasswordToken == tokenFromSession && u.ResetPasswordTokenExpiration > DateTime.UtcNow);
            if (user == null)
            {
                // Invalid or expired token
                return RedirectToAction("ResetPassword");
            }

            // Update password
            user.Password = MD5Hash.CalculateMD5Hash(us.Password);
            user.ResetPasswordToken = null; // Reset token
                                            // user.ResetPasswordTokenExpiration = null; // Reset token expiration
            _db.Entry(user).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            // Redirect to login page or a password reset success page
            return RedirectToAction("Login", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> ResendOTPForgetPassWord(string email)
        {
            var oldOTPs = await _db.oTPs.Where(x => x.Email == email).ToListAsync();
            if (oldOTPs.Any())
            {
                _db.oTPs.RemoveRange(oldOTPs);
                await _db.SaveChangesAsync();
            }
            var otpCode = GenerateOTP();
            var newOtp = new OTP
            {
                Email = email,
                Code = otpCode,
                ExpirationTime = DateTime.Now.AddMinutes(2)
            };
            this._db.oTPs.Add(newOtp);
            await this._db.SaveChangesAsync();
            var mailContent = new MailContent
            {
                To = email,
                Subject = "New OTP Code Forget PassWord",
                Body = $"<h1>Here is your new OTP code:</h1><p>{otpCode}</p>"
            };
            var result = await _sendMailService.SendMail(mailContent);
            if (result.StartsWith("Error"))
            {
                return Json(new { success = false, message = result });

            }
            HttpContext.Session.SetString("OTP", JsonConvert.SerializeObject(newOtp));

            return Json(new { success = true });

        }


        public IActionResult Logout()
        {
            // Lưu giá trị UserName và Email vào biến tạm thời
            var userName = HttpContext.Session.GetString("RememberedUserName");
            var email = HttpContext.Session.GetString("RememberedEmail");
            // Xóa các thông tin phiên (session) hoặc cookie cần thiết
            HttpContext.Session.Clear();

            // Chuyển hướng đến trang Login
            return RedirectToAction("Login", "Home", new { RememberedUserName = userName, RememberedEmail = email });
        }

        public IActionResult AccessDenied()
        {
            // TODO: Your code here
            return View();
        }


    }
}
