using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Core;
using Application.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Profiles
{
    public class ListEvents
    {
        public class Query : IRequest<Result<List<UserEventDto>>> {
            public string Username { get; set; }
            public string Predicate { get; set; }
        }

        public class Handler : IRequestHandler<Query, Result<List<UserEventDto>>>
        {
            private readonly DataContext _context;
            private readonly IUserAccessor _userAccessor;
            private readonly IMapper _mapper;
            public Handler(DataContext context, IMapper mapper, IUserAccessor userAccessor)
            {
                _mapper = mapper;
                _context = context;
                _userAccessor = userAccessor;
            }

            public async Task<Result<List<UserEventDto>>> Handle(Query request, CancellationToken cancellationToken)
            {
                var query = _context.EventAttendees
                    .Where(u => (u.AppUser.UserName == request.Username) && (u.Event.Anonimity != "PRIVATE" || u.Event.Attendees.Any(x => x.AppUser.UserName.ToLower() == _userAccessor.GetUsername().ToLower())))
                    .OrderBy(a => a.Event.Date)
                    .ProjectTo<UserEventDto>(_mapper.ConfigurationProvider)
                    .AsQueryable();

                query = request.Predicate switch {
                    "past" => query.Where(a => a.Date.ToUniversalTime() <= DateTime.Now.ToUniversalTime()),
                    "hosting" => query.Where(a => a.HostUsername == request.Username),
                    _ => query.Where(a => a.Date.ToUniversalTime() >= DateTime.Now.ToUniversalTime())
                };

                var events = await query.ToListAsync();

                return Result<List<UserEventDto>>.Success(events);
            }
        }
    }
}