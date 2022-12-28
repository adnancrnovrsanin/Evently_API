using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Profiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class ProfilesController : BaseApiController
    {
        [AllowAnonymous]
        [HttpGet("{username}")]
        public async Task<IActionResult> GetProfile(string username) {
            return HandleResult(await Mediator.Send(new Details.Query{ Username = username }));
        }

        [HttpPut]
        public async Task<IActionResult> Edit(Edit.Command command) {
            return HandleResult(await Mediator.Send(command));
        }

        [HttpDelete]
        public async Task<IActionResult> Delete() {
            return HandleResult(await Mediator.Send(new Delete.Command()));
        }

        [AllowAnonymous]
        [HttpGet("{username}/events")]
        public async Task<IActionResult> GetUserEvents(string username, string predicate) {
            return HandleResult(await Mediator.Send(new ListEvents.Query {
                Username = username,
                Predicate = predicate
            }));
        }
    }
}