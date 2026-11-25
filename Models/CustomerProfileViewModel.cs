
using System.ComponentModel.DataAnnotations;

namespace HomeEase.Models
{
    public class CustomerProfileViewModel
    {
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Phone]
        [StringLength(15)]
        public string Phone { get; set; }
    }
}
