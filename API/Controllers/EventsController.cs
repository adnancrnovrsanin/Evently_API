using Application.Events;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class EventsController : BaseApiController
    {
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetEvents([FromQuery]EventParams param) {
            return HandlePagedResult(await Mediator.Send(new List.Query{ Params = param }));
        }

        [AllowAnonymous]
        [HttpGet("{id}")] // single Event
        public async Task<IActionResult> GetEvent(Guid id) {
            return HandleResult(await Mediator.Send(new Details.Query{ Id = id }));
        }

        [HttpPost]
        public async Task<IActionResult> CreateEvent(Event newEvent) {
            return HandleResult(await Mediator.Send(new Create.Command{ Event = newEvent }));
        }

        [Authorize(Policy = "IsEventHost")]
        [HttpPut("{id}")]
        public async Task<IActionResult> EditEvent(Guid id, Event newEvent) {
            newEvent.Id = id;
            return HandleResult(await Mediator.Send(new Edit.Command{ Event = newEvent }));
        }

        [Authorize(Policy = "IsEventHost")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(Guid id) {
            return HandleResult(await Mediator.Send(new Delete.Command{ Id = id }));
        }

        [HttpPost("{id}/attend")]
        public async Task<IActionResult> Attend(Guid id) {
            return HandleResult(await Mediator.Send(new UpdateAttendance.Command{ Id = id }));
        }

        [HttpPost("{id}/reportHost")]
        public async Task<IActionResult> ReportHost(Guid id) {
            return HandleResult(await Mediator.Send(new ReportHost.Command{ Id = id }));
        }

        [HttpPost("{id}/requestInvite")]
        public async Task<IActionResult> RequestInvite(Guid id) {
            return HandleResult(await Mediator.Send(new RequestInvite.Command{ Id = id }));
        }

        [Authorize(Policy = "IsEventHost")]
        [HttpDelete("{id}/removeRequest")]
        public async Task<IActionResult> RemoveRequest(Guid id, string username) {
            return HandleResult(await Mediator.Send(new RemoveInviteRequest.Command{ EventId = id, Username = username }));
        }

        [Authorize(Policy = "IsEventHost")]
        [HttpPost("{id}/acceptRequest")]
        public async Task<IActionResult> AcceptRequest(Guid id, string username) {
            return HandleResult(await Mediator.Send(new AcceptRequest.Command{ EventId = id, Username = username }));
        }

        
    }
}