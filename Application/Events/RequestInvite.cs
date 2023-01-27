using Application.Core;
using Application.Interfaces;
using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Events
{
    public class RequestInvite
    {
        public class Command : IRequest<Result<Unit>> {
            public Guid Id { get; set; }
        }

        public class Handler : IRequestHandler<Command, Result<Unit>>
        {
            private readonly IUserAccessor _userAccessor;
            private readonly DataContext _context;

            public Handler(DataContext context, IUserAccessor userAccessor)
            {
                _userAccessor = userAccessor;
                _context = context;
            }

            public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
            {
                var newEvent = await _context.Events
                    .Include(i => i.InviteRequests).ThenInclude(u => u.AppUser)
                    .SingleOrDefaultAsync(x => x.Id == request.Id);
                
                if (newEvent == null) return null;

                var user = await _context.Users.FirstOrDefaultAsync(x => x.UserName == _userAccessor.GetUsername());

                if (user == null) return null;

                var attendeeExists = newEvent.Attendees.FirstOrDefault(x => x.AppUser.UserName == user.UserName);

                if (attendeeExists != null) return Result<Unit>.Failure("You are already attending this event");

                var requestExists = newEvent.InviteRequests.FirstOrDefault(x => x.AppUser.UserName == user.UserName);

                if (requestExists != null) return Result<Unit>.Failure("You have already requested to join this event");

                if (newEvent.Anonimity != "ON INVITE") return Result<Unit>.Failure("This event is not open to invites");

                var inviteRequest = new InviteRequest{
                    AppUser = user,
                    Event = newEvent,
                    CreatedAt = DateTime.UtcNow
                };

                newEvent.InviteRequests.Add(inviteRequest);

                var result = await _context.SaveChangesAsync() > 0;

                if (!result) return Result<Unit>.Failure("Failed to request invite");

                return Result<Unit>.Success(Unit.Value);
            }
        }
    }
}