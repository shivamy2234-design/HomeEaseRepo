using System.ComponentModel.DataAnnotations;

namespace HomeEase.Models
{
    public class AdminServiceViewModel
    {
        public int ServiceId { get; set; }

        [Required(ErrorMessage = "Service name is required.")]
        [StringLength(100)]
        [Display(Name = "Service Name")]
        public string ServiceName { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        [StringLength(50)]
        public string Category { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Range(typeof(decimal), "0.01", "99999999",
            ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; }

        [Range(1, 1440,
            ErrorMessage = "Duration must be between 1 and 1440 minutes.")]
        [Display(Name = "Duration (minutes)")]
        public int DurationMinutes { get; set; }

        [Display(Name = "Active Service")]
        public bool IsActive { get; set; }
    }
}