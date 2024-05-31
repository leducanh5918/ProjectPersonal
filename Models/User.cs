using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace projectPersonal.Models
{
    public class User
    {
        public string? First { get; set; }
        public string? lastName { get; set; }
        public DateTime? dateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Address { get; set; }
        public int? Zipcode { get; set; }
        public int Phonenumber { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        [Key]
        public int IDCard { get; set; }
        public string Password { get; set; }
        [NotMapped]
        [Compare("Password", ErrorMessage = "Password and Confirm Password do not match")]
        public string ConfirmPassword { get; set; }
        public string? Image { get; set; }
        [ForeignKey("RoleID")]
        [DisplayName("Please select the role to register into the system")]
        public int RoleID { get; set; }
        public Role Role { get; set; }
        public DateTime? registrationDate { get; set; }
        public int? Lockout { get; set; }
        public string? ResetPasswordToken { get; set; }
        public DateTime ResetPasswordTokenExpiration { get; set; }
    }
}