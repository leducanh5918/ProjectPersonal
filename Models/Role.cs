using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace projectPersonal.Models
{
    public class Role
    {
        [Key]
        public int RoleID{ get; set; }
        public string Name { get; set; }
        public Collection<User> users{ get; set; }
    }
}