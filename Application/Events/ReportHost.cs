using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Core;
using Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Events
{
    public class ReportHost
    {
        public class Command : IRequest<Result<Unit>> {
            public Guid Id { get; set; }
        }

        public class Handler : IRequestHandler<Command, Result<Unit>>
        {
        private readonly DataContext _context;
        private readonly IUserAccessor _userAccessor;
            public Handler(DataContext context, IUserAccessor userAccessor)
            {
                _userAccessor = userAccessor;
                _context = context;
            }

            public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
            {
                var eventHost = await _context.EventAttendees
                    .Include(a => a.AppUser)
                    .SingleOrDefaultAsync(x => (x.EventId == request.Id) && (x.IsHost == true));
                
                if (eventHost == null) return null;

                eventHost.AppUser.Reported = true;

                var result = await _context.SaveChangesAsync() > 0;

                if (result) return Result<Unit>.Success(Unit.Value);

                return Result<Unit>.Failure("Failed to report host");
            }
        }
    }
}