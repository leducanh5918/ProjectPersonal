using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using projectPersonal.Models;

namespace projectPersonal.Data
{
    public class ApplicationDBcontext : DbContext
    {
        public ApplicationDBcontext(DbContextOptions<ApplicationDBcontext> options) : base(options)
        {

        }
        public DbSet<User> users { get; set; }
        public DbSet <Role> roles { get; set; }
        public DbSet<OTP> oTPs{ get; set; }
    }
}