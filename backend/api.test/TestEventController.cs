using System.Collections.Generic;
using Api.Context;
using Api.Controllers;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Api.Test
{
    [Collection("Database collection")]
    public class TestEventController
    {
        private readonly EventController _controller;

        public TestEventController()
        {
            var options = new DbContextOptionsBuilder().UseInMemoryDatabase("flotilla").Options;
            var context = new FlotillaDbContext(options);
            var service = new EventService(context);
            var logger = new LoggerFactory().CreateLogger<EventController>();
            _controller = new EventController(logger, service);
        }

        [Fact]
        public void GetEvents()
        {
            var result = (OkObjectResult)_controller.GetEvents().Result.Result!;
            var events = (IList<Event>?)result.Value;

            Assert.NotNull(events);
            if (events is null)
                return;

            Assert.Equal(InitDb.Events.Count, events.Count);
        }

        [Fact]
        public void GetEventById_ShouldReturnNotFound()
        {
            var actionResultType = typeof(NotFoundObjectResult);
            string eventId = "RandomString";

            IActionResult? result = _controller.GetEventById(eventId).Result.Result;

            // Check if the result is, or inherits from, the expected result type
            Assert.IsAssignableFrom(actionResultType, result);
        }

        [Fact]
        public void GetEventById_ShouldReturnOkObject()
        {
            var actionResultType = typeof(OkObjectResult);
            var result = (OkObjectResult)_controller.GetEvents().Result.Result!;
            var events = (IList<Event>?)result.Value;
            Assert.NotNull(events);

            if (events is null)
                return;

            string eventId = events[0].Id;

            IActionResult? actionResult = _controller.GetEventById(eventId).Result.Result;

            // Check if the result is, or inherits from, the expected result type
            Assert.IsAssignableFrom(actionResultType, actionResult);
        }

        [Fact]
        public void DeleteEvent_ShouldReturnNotFound()
        {
            var actionResultType = typeof(NotFoundObjectResult);
            string eventId = "RandomString";

            IActionResult? result = _controller.DeleteEvent(eventId).Result.Result;

            // Check if the result is, or inherits from, the expected result type
            Assert.IsAssignableFrom(actionResultType, result);
        }

        [Fact]
        public void DeleteEvent_ShouldReturnOkObject()
        {
            var actionResultType = typeof(OkObjectResult);
            var result = (OkObjectResult)_controller.GetEvents().Result.Result!;
            var events = (IList<Event>?)result.Value;
            Assert.NotNull(events);

            if (events is null)
                return;

            string eventId = events[0].Id;

            IActionResult? actionResult = _controller.DeleteEvent(eventId).Result.Result;

            // Check if the result is, or inherits from, the expected result type
            Assert.IsAssignableFrom(actionResultType, actionResult);
        }
    }
}
