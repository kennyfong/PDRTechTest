using PDR.PatientBooking.Data.Models;
using System;

namespace PDR.PatientBooking.Service.BookingServices.Requests
{
    public class AddBookingRequest
    {
        public Guid Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public long PatientId { get; set; }
        public long DoctorId { get; set; }
    }
}
