using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace projectPersonal.Models
{
    public class OTPChangePassWord
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string OTPCode { get; set; }
        public DateTime ExpirationTime { get; set; }
        public string NewPassword { get; set; }
        [Compare("NewPassword", ErrorMessage = "NewPassword and Confirm Password do not match")]
        public string ConfirmPassword { get; set; }
    }
}