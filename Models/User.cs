using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ProjectManagerPro_SOLID.Enums;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectManagerPro_SOLID.Models
{
    public class User
    {
        public int Id { get; set; }
        public string NodeID { get; set; }
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public UserRole Role { get; set; } // Developer, Tester, Reviewer

    }
}
