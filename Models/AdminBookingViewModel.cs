namespace HomeEase.Models
{
    public class AdminBookingViewModel
    {
        public int BookingId { get; set; }

        public string CustomerName { get; set; }

        public string CustomerEmail { get; set; }

        public string ServiceName { get; set; }

        public System.DateTime BookingDate { get; set; }


        public string TimeSlot { get; set; }

        public string Status { get; set; }

        public decimal TotalAmount { get; set; }
        public int? StaffProfileId { get; set; }

        public string AssignedStaffName { get; set; }


    }

}
