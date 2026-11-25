using System;

namespace HomeEase.Models
{
    public class BookingHistoryViewModel
    {
        public int BookingId { get; set; }

        public string ServiceName { get; set; }

        public DateTime BookingDate { get; set; }

        public string TimeSlot { get; set; }

        public string Status { get; set; }

        public decimal TotalAmount { get; set; }
        public bool HasReview { get; set; }

        public bool HasPaid { get; set; }


    }
}