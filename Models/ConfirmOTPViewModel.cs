using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace projectPersonal.Models
{
    public class ConfirmOTPViewModel
    {
        public string Email { get; set; }
        public string OTPCode { get; set; }
         public DateTime ExpirationTime { get; set; }
    }
}