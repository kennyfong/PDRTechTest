using AutoFixture;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using PDR.PatientBooking.Data;
using PDR.PatientBooking.Data.Models;
using PDR.PatientBooking.Service.BookingServices.Requests;
using PDR.PatientBooking.Service.BookingServices.Validation;
using System;

namespace PDR.PatientBooking.Service.Tests.BookingServices.Validation
{
    [TestFixture]
    public class AddBookingRequestValidatorTests
    {
        private MockRepository _mockRepository;
        private IFixture _fixture;

        private PatientBookingContext _context;

        private AddBookingRequestValidator _addBookingRequestValidator;

        [SetUp]
        public void SetUp()
        {
            // Boilerplate
            _mockRepository = new MockRepository(MockBehavior.Strict);
            _fixture = new Fixture();

            //Prevent fixture from generating from entity circular references 
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior(1));

            // Mock setup
            _context = new PatientBookingContext(new DbContextOptionsBuilder<PatientBookingContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

            // default context
            SetupDefaultContext();

            // Sut instantiation
            _addBookingRequestValidator = new AddBookingRequestValidator(
                _context
            );
        }

        private void SetupDefaultContext()
        {
            var order = _fixture.Build<Order>()
                .With(b => b.DoctorId, 1)
                .With(b => b.StartTime, new DateTime(2022,3,23,0,0,0))
                .With(b => b.EndTime, new DateTime(2022,3,23,1,0,0))
                .Create();
            _context.Order.Add(order);
            _context.SaveChanges();
        }

        [Test]
        public void ValidateRequest_AllChecksPass_ReturnsPassedValidationResult()
        {
            //arrange
            var request = GetValidRequest();
            request.DoctorId = 2;

            //act
            var res = _addBookingRequestValidator.ValidateRequest(request);

            //assert
            res.PassedValidation.Should().BeTrue();
        }

        [Test]
        public void ValidateRequest_AllChecksPassWithExistingDoctorBooked_ReturnsPassedValidationResult()
        {
            //arrange
            var request = GetValidRequest();
            request.DoctorId = 1;
            request.StartTime = new DateTime(2022,3,23,1,30,00);
            request.EndTime = new DateTime(2022,3,23,2,30,00);

            //act
            var res = _addBookingRequestValidator.ValidateRequest(request);

            //assert
            res.PassedValidation.Should().BeTrue();
        }

        [TestCase("2020-03-23 00:00:00", "2020-03-23 01:00:00")]
        [TestCase("2020-03-23 00:00:00", "2022-03-23 01:00:00")]
        [TestCase("2022-03-23 00:00:00", "2020-03-23 01:00:00")]
        public void ValidateRequest_BookingInThePast_ReturnsFailedValidationResult(DateTime bookingStartTime, DateTime bookingEndTime)
        {
            //arrange
            var request = GetValidRequest();
            request.StartTime = bookingStartTime;
            request.EndTime = bookingEndTime;

            //act
            var res = _addBookingRequestValidator.ValidateRequest(request);

            string[] expectedErrors = { "Start Time must be set in the past", "End Time must be set in the past" };

            //assert
            res.PassedValidation.Should().BeFalse();
            res.Errors.Should().IntersectWith(expectedErrors);
        }

        [TestCase("1","2022-03-23 00:00:00", "2022-03-23 01:00:00")]
        [TestCase("1", "2022-03-23 00:15:00", "2022-03-23 01:15:00")]
        [TestCase("1", "2022-03-22 23:45:00", "2022-03-23 00:15:00")]
        public void ValidateRequest_BookingWithSameDoctorAtSameTime_ReturnsFailedValidationResult(long doctorId, DateTime bookingStartTime, DateTime bookingEndTime)
        {
            //arrange
            var request = GetValidRequest();
            request.DoctorId = doctorId;
            request.StartTime = bookingStartTime;
            request.EndTime = bookingEndTime;

            //act
            var res = _addBookingRequestValidator.ValidateRequest(request);

            //assert
            res.PassedValidation.Should().BeFalse();
            res.Errors.Should().Contain("A doctor is currently booked in the date time range specified");
        }

        private AddBookingRequest GetValidRequest()
        {
            var request = _fixture.Build<AddBookingRequest>()
                .With(b => b.StartTime, DateTime.Now.AddDays(1))
                .With(b => b.EndTime, DateTime.Now.AddDays(1).AddMinutes(30))
                .Create();
            return request;
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
        }
    }
}
