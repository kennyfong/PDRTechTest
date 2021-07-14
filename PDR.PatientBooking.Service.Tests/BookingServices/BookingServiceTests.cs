using AutoFixture;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using PDR.PatientBooking.Data;
using PDR.PatientBooking.Data.Models;
using PDR.PatientBooking.Service.BookingServices;
using PDR.PatientBooking.Service.BookingServices.Requests;
using PDR.PatientBooking.Service.BookingServices.Validation;
using PDR.PatientBooking.Service.Validation;
using System;

namespace PDR.PatientBooking.Service.Tests.BookingServices
{
    [TestFixture]
    public class BookingServiceTests
    {
        private MockRepository _mockRepository;
        private IFixture _fixture;

        private PatientBookingContext _context;
        private Mock<IAddBookingRequestValidator> _validator;

        private BookingService _bookingService;

        [SetUp]
        public void SetUp()
        {
            // Boilerplate
            _mockRepository = new MockRepository(MockBehavior.Strict);
            _fixture = new Fixture();

            //Prevent fixture from generating circular references
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior(1));

            // Mock setup
            _context = new PatientBookingContext(new DbContextOptionsBuilder<PatientBookingContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            _validator = _mockRepository.Create<IAddBookingRequestValidator>();

            // Mock default
            SetupMockDefaults();

            // Sut instantiation
            _bookingService = new BookingService(
                _context,
                _validator.Object
            );
        }

        private void SetupMockDefaults()
        {
            _validator.Setup(x => x.ValidateRequest(It.IsAny<AddBookingRequest>()))
                .Returns(new PdrValidationResult(true));
        }

        [Test]
        public void AddOrder_ValidatesRequest()
        {
            //arrange
            var patientRequest = _fixture.Create<Patient>();
            _context.Patient.Add(patientRequest);
            _context.SaveChanges();

            var request = _fixture.Build<AddBookingRequest>()
                .With(o => o.PatientId, patientRequest.Id)
                .Create();

            //act
            _bookingService.AddOrder(request);

            //assert
            _validator.Verify(x => x.ValidateRequest(request), Times.Once);
        }

        [Test]
        public void AddOrder_NoPatient_ThrowsArgumentException()
        {
            //act
            var exception = Assert.Throws<ArgumentNullException>(() => _bookingService.GetPatientNextAppointment(1));

            //assert
            exception.Message.Should().Contain("Patient does not exist");
        }

        [Test]
        public void AddOrder_NoNextAppointment_ThrowsArgumentException()
        {
            DateTime startTime = DateTime.Now.AddDays(-1);
            DateTime endTime = DateTime.Now.AddDays(-1).AddMinutes(30);

            var request = _fixture.Create<AddBookingRequest>();
            var order = _fixture.Build<Order>()
                .With(o => o.StartTime, startTime)
                .With(o => o.StartTime, endTime)
                .With(o => o.IsCancelled, false)
                .Create();
            _context.Order.Add(order);
            _context.SaveChanges();

            //act
            var exception = Assert.Throws<ArgumentNullException>(() => _bookingService.GetPatientNextAppointment(order.PatientId));

            //assert
            exception.Message.Should().Contain("There are no next appointment");
        }

        [Test]
        public void AddOrder_AddsOrderToContextWithGeneratedId()
        {
            //arrange
            var patientRequest = _fixture.Create<Patient>();
            _context.Patient.Add(patientRequest);
            _context.SaveChanges();

            var request = _fixture.Build<AddBookingRequest>()
                .With(o => o.PatientId, patientRequest.Id)
                .Create();

            var expected = new AddBookingRequest
            {
                 DoctorId = request.DoctorId,
                 EndTime = request.EndTime,
                 StartTime = request.StartTime,
                 PatientId = patientRequest.Id
            };

            //act
            _bookingService.AddOrder(request);

            //assert
            _context.Order.Should().ContainEquivalentOf(expected, options => options
                .Excluding(order => order.Id));
        }

        [Test]
        public void GetPatientNextAppointment_ReturnsNextAppointment()
        {
            DateTime startTime = DateTime.Now.AddDays(1);
            DateTime endTime = DateTime.Now.AddDays(1).AddMinutes(30);

            //arrange
            var order = _fixture.Build<Order>()
                .With(o => o.StartTime, startTime)
                .With(o => o.StartTime, endTime)
                .With(o => o.IsCancelled, false)
                .Create();
            _context.Order.Add(order);
            _context.SaveChanges();

            //act
            var res = _bookingService.GetPatientNextAppointment(order.PatientId);

            //assert
            res.StartTime.Should().Equals(startTime);
        }

        [Test]
        public void CancelAppointment_ReturnsSuccessful()
        {
            DateTime startTime = DateTime.Now.AddDays(1);
            DateTime endTime = DateTime.Now.AddDays(1).AddMinutes(30);

            //arrange
            var order = _fixture.Build<Order>()
                .With(o => o.StartTime, startTime)
                .With(o => o.StartTime, endTime)
                .With(o => o.IsCancelled, false)
                .Create();
            _context.Order.Add(order);
            _context.SaveChanges();

            var expected = new Order
            {
                DoctorId = order.DoctorId,
                EndTime = order.EndTime,
                StartTime = order.StartTime,
                PatientId = order.PatientId,
                IsCancelled = true
            };

            //act
            _bookingService.CancelOrder(order.Id);

            //assert
            _context.Order.Should().ContainEquivalentOf(expected, options => options
                .Excluding(order => order.Doctor)
                .Excluding(order => order.Patient)
                .Excluding(order => order.SurgeryType)
                .Excluding(order => order.Id));
        }

        [Test]
        public void GetPatientNextAppointmentAfterMultipleAppointmentsAndCancelNext_ReturnsNextAppointment()
        {
            DateTime startTime = DateTime.Now.AddDays(1);
            DateTime endTime = DateTime.Now.AddDays(1).AddMinutes(30);

            //arrange
            var order = _fixture.Build<Order>()
                .With(o => o.StartTime, startTime)
                .With(o => o.StartTime, endTime)
                .With(o => o.IsCancelled, false)
                .Create();
            _context.Order.Add(order);
            _context.SaveChanges();

            //act
            var res = _bookingService.GetPatientNextAppointment(order.PatientId);

            //assert
            res.StartTime.Should().Equals(startTime);
        }

        [Test]
        public void GetPatientNextAppointmentAfterOneAppointmentAndCancellation_ThrowsException()
        {
            DateTime startTime = DateTime.Now.AddDays(1);
            DateTime endTime = DateTime.Now.AddDays(1).AddMinutes(30);

            //arrange
            var order = _fixture.Build<Order>()
                .With(o => o.StartTime, startTime)
                .With(o => o.StartTime, endTime)
                .With(o => o.IsCancelled, true)
                .Create();
            _context.Order.Add(order);

            _context.SaveChanges();

            //assert
            var exception = Assert.Throws<ArgumentNullException>(() => _bookingService.GetPatientNextAppointment(order.PatientId));

            //assert
            exception.Message.Should().Contain("There are no next appointment");
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
        }
    }
}
