
using System.ComponentModel.DataAnnotations;

namespace HomeEase.Models
{
    public class ReviewViewModel
    {
        public int BookingId { get; set; }

        [Range(1, 5, ErrorMessage = "Please select a rating from 1 to 5.")]
        public int Rating { get; set; }

        [StringLength(500, ErrorMessage = "Comment cannot exceed 500 characters.")]
        [Display(Name = "Your Review")]
        public string Comment { get; set; }

        public string ServiceName { get; set; }
    }
}
