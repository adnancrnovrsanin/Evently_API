using Application.Core;
using Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Events
{
    public class RemoveInviteRequest
    {
        public class Command : IRequest<Result<Unit>> {
            public Guid EventId { get; set; }
            public string Username { get; set; }
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
                    .SingleOrDefaultAsync(x => x.Id == request.EventId);
                
                if (newEvent == null) return null;

                var user = await _context.Users.FirstOrDefaultAsync(x => x.UserName == request.Username);

                if (user == null) return null;

                var isAttendee = newEvent.Attendees.FirstOrDefault(x => x.AppUser.UserName == user.UserName);

                if (isAttendee == null) return Result<Unit>.Failure("This user is not an attendee of this event");

                isAttendee = newEvent.Attendees.FirstOrDefault(x => x.AppUser.UserName == _userAccessor.GetUsername());

                if (isAttendee == null || (isAttendee.AppUser.UserName != request.Username && !isAttendee.IsHost)) return Result<Unit>.Failure("You are not allowed to remove this request");

                var requestExists = newEvent.InviteRequests.FirstOrDefault(x => x.AppUser.UserName == user.UserName);

                if (requestExists == null) return Result<Unit>.Failure("This user has not requested to join this event");

                newEvent.InviteRequests.Remove(requestExists);

                var result = await _context.SaveChangesAsync() > 0;

                if (!result) return Result<Unit>.Failure("Failed to remove invite request");

                return Result<Unit>.Success(Unit.Value);
            }
        }
    }
}