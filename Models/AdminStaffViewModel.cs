
using System.ComponentModel.DataAnnotations;

namespace HomeEase.Models
{
    public class AdminStaffViewModel
    {
        public int UserId { get; set; }

        public int StaffProfileId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; }

        [StringLength(20)]
        public string Phone { get; set; }

        [StringLength(500)]
        public string Skills { get; set; }

        [Range(0, 50)]
        [Display(Name = "Experience (Years)")]
        public int ExperienceYears { get; set; }

        public bool IsAvailable { get; set; }

        public bool IsActive { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(100, MinimumLength = 8)]
        [System.ComponentModel.DataAnnotations.DataType(
            System.ComponentModel.DataAnnotations.DataType.Password)]
        public string Password { get; set; }

    }
}
