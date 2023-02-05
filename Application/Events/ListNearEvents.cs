using Application.Core;
using Application.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Events
{
    public class ListNearEvents
    {
        public class Query : IRequest<Result<List<EventDto>>> {
            public NearbyEventParams Params { get; set; }
        }

        public class Handler : IRequestHandler<Query, Result<List<EventDto>>>
        {
            private readonly IMapper _mapper;
            private readonly DataContext _context;
            private readonly IUserAccessor _userAccessor;

            public Handler(DataContext context, IMapper mapper, IUserAccessor userAccessor)
            {
                _mapper = mapper;
                _context = context;
                _userAccessor = userAccessor;
            }

            public async Task<Result<List<EventDto>>> Handle(Query request, CancellationToken cancellationToken)
            {
                var events = await _context.Events
                    .Where(x => x.Attendees.All(a => a.AppUser.UserName != _userAccessor.GetUsername()) && (x.City.ToLower() == request.Params.City.ToLower() || x.Country.ToLower() == request.Params.Country.ToLower()) && x.Anonimity != "PRIVATE" && x.Date.ToUniversalTime() >= request.Params.StartDate.ToUniversalTime())
                    .OrderBy(d => d.Date)
                    .ProjectTo<EventDto>(_mapper.ConfigurationProvider, new { currentUsername = _userAccessor.GetUsername() })
                    .ToListAsync();

                if (events == null)
                    return null;
                
                return Result<List<EventDto>>.Success(events);
            }
        }
    }
}