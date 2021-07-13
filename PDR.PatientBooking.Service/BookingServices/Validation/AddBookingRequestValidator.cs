using PDR.PatientBooking.Data;
using PDR.PatientBooking.Service.BookingServices.Requests;
using PDR.PatientBooking.Service.Validation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PDR.PatientBooking.Service.BookingServices.Validation
{
    public class AddBookingRequestValidator : IAddBookingRequestValidator
    {
        private readonly PatientBookingContext _context;

        public AddBookingRequestValidator(PatientBookingContext context)
        {
            _context = context;
        }

        public PdrValidationResult ValidateRequest(AddBookingRequest request)
        {
            var result = new PdrValidationResult(true);

            if (CheckBookingInPast(request, ref result))
                return result;

            if (CheckDoctorIsntBooked(request, ref result))
                return result;

            return result;
        }

        public bool CheckBookingInPast(AddBookingRequest request, ref PdrValidationResult result)
        {
            var errors = new List<string>();

            if (request.StartTime < DateTime.Now)
                errors.Add("Start Time must be set in the past");

            if (request.EndTime < DateTime.Now)
                errors.Add("End Time must be set in the past");

            if (errors.Any())
            {
                result.PassedValidation = false;
                result.Errors.AddRange(errors);
                return true;
            }

            return false;
        }

        private bool CheckDoctorIsntBooked(AddBookingRequest request, ref PdrValidationResult result)
        {
            if (_context.Order.Any(x => x.DoctorId == request.DoctorId && 
            (request.StartTime >= x.StartTime && request.StartTime <= x.EndTime) ||
            (request.EndTime >= x.StartTime && request.StartTime <= x.EndTime)))
            {
                result.PassedValidation = false;
                result.Errors.Add("A doctor is currently booked in the date time range specified");
                return true;
            }

            return false;
        }
    }
}
