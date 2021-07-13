using PDR.PatientBooking.Service.BookingServices.Requests;
using PDR.PatientBooking.Service.BookingServices.Responses;

namespace PDR.PatientBooking.Service.BookingServices
{
    public interface IBookingService
    {
        void AddOrder(AddBookingRequest request);
        GetNextAppointmentResponse GetPatientNextAppointment(long identificationNumber);
    }
}