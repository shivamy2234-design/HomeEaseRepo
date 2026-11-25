using System;
using System.ComponentModel.DataAnnotations;

namespace HomeEase.Models
{
    public class BookingViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid service.")]
        public int ServiceId { get; set; }

        [Display(Name = "Service")]
        public string ServiceName { get; set; }

        public decimal TotalAmount { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Booking Date")]
        public DateTime BookingDate { get; set; } = DateTime.Today.AddDays(1);

        [Required]
        [StringLength(50)]
        [Display(Name = "Time Slot")]
        public string TimeSlot { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please select an address.")]
        [Display(Name = "Service Address")]
        public int AddressId { get; set; }

        [StringLength(500)]
        [Display(Name = "Special Instructions")]
        public string SpecialInstructions { get; set; }
    }
}
