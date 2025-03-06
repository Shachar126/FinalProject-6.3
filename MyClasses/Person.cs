using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyClasses
{
    public enum RoleIn
    {
        Guest,
        User,
        Admin
    }

    public class Person
    {
        [Key] // Primary key
        public int user_id { get; set; }

        [Required]
        [MaxLength(50)]
        public string username { get; set; }

        [Required]
        [MaxLength(255)]
        public string password { get; set; }

        [Required]
        public DateTime created_at { get; set; }

        [Required]
        [MaxLength(50)]
        public string first_name { get; set; }

        [Required]
        [MaxLength(50)]
        public string last_name { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string email { get; set; }

        [Required]
        public RoleIn role { get; set; }




        // Constructor for initialization
        public Person() { }

        public Person(int user_id, string username, string password, DateTime created_at,
                      string first_name, string last_name, string email, RoleIn role)
        {
            this.user_id = user_id;
            this.username = username;
            this.password = password;
            this.created_at = created_at;
            this.first_name = first_name;
            this.last_name = last_name;
            this.email = email;
            this.role = role;
        }
    }
}