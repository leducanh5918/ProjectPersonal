using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace projectPersonal.Models
{
    public class OTP
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Code { get; set; }
        public DateTime ExpirationTime { get; set; }
    }
}