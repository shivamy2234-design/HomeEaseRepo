
using System.ComponentModel.DataAnnotations;

namespace HomeEase.Models
{
    public class AddressViewModel
    {
        public int AddressId { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Address")]
        public string AddressLine { get; set; }

        [Required]
        [StringLength(100)]
        public string City { get; set; }

        [Required]
        [StringLength(100)]
        public string State { get; set; }

        [Required]
        [StringLength(10)]
        [RegularExpression(@"^[0-9]{6}$",
            ErrorMessage = "Enter a valid 6-digit PIN code.")]
        [Display(Name = "PIN Code")]
        public string PostalCode { get; set; }

        [Display(Name = "Set as default address")]
        public bool IsDefault { get; set; }
    }
}
