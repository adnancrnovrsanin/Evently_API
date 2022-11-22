using Application.Events;
using Domain;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class EventsController : BaseApiController
    {
        [HttpGet]
        public async Task<ActionResult<List<Event>>> GetEvents() {
            return await Mediator.Send(new List.Query());
        }

        [HttpGet("{id}")] // single Event
        public async Task<ActionResult<Event>> GetEvent(Guid id) {
            return await Mediator.Send(new Details.Query{ Id = id });
        }

        [HttpPost]
        public async Task<IActionResult> CreateEvent(Event newEvent) {
            return Ok(await Mediator.Send(new Create.Command{ Event = newEvent }));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditEvent(Guid id, Event newEvent) {
            newEvent.Id = id;
            return Ok(await Mediator.Send(new Edit.Command{ Event = newEvent }));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(Guid id) {
            return Ok(await Mediator.Send(new Delete.Command{ Id = id }));
        }
    }
}