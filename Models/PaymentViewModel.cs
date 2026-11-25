using System;

namespace HomeEase.Models
{
    public class PaymentViewModel
    {
        public int BookingId { get; set; }

        public string ServiceName { get; set; }

        public DateTime BookingDate { get; set; }

        public string TimeSlot { get; set; }

        public decimal Amount { get; set; }

        public string PaymentStatus { get; set; }

        public string RazorpayOrderId { get; set; }

        public string RazorpayKeyId { get; set; }
    }
}
