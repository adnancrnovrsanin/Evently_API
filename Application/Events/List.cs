using Application.Core;
using Application.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Events
{
    public class List
    {
        public class Query : IRequest<Result<PagedList<EventDto>>> {
            public EventParams Params { get; set; }
        }

        public class Handler : IRequestHandler<Query, Result<PagedList<EventDto>>>
        {
            private readonly DataContext _context;
            private readonly IMapper _mapper;
            private readonly IUserAccessor _userAccessor;

            public Handler(DataContext context, IMapper mapper, IUserAccessor userAccessor)
            {
                _mapper = mapper;
                _context = context;
                _userAccessor = userAccessor;
            }

            public async Task<Result<PagedList<EventDto>>> Handle(Query request, CancellationToken cancellationToken)
            {
                var query = _context.Events
                    .Where(d => (d.Date >= request.Params.StartDate) && (d.Anonimity != "PRIVATE" || d.Attendees.Any(x => x.AppUser.UserName.ToLower() == _userAccessor.GetUsername().ToLower())))
                    .OrderBy(d => d.Date)
                    .ProjectTo<EventDto>(_mapper.ConfigurationProvider, new { currentUsername = _userAccessor.GetUsername() })
                    .AsQueryable();

                
                
                if (request.Params.SearchQuery != null) {
                    query = query.Where(x => 
                        x.Title.ToLower().Contains(request.Params.SearchQuery.ToLower()) ||
                        x.HostDisplayName.ToLower().Contains(request.Params.SearchQuery.ToLower()) ||
                        x.Description.ToLower().Contains(request.Params.SearchQuery.ToLower()) || 
                        x.Category.ToLower().Contains(request.Params.SearchQuery.ToLower()) ||
                        x.City.ToLower().Contains(request.Params.SearchQuery.ToLower()) ||
                        x.Venue.ToLower().Contains(request.Params.SearchQuery.ToLower()) ||
                        x.HostUsername.ToLower().Contains(request.Params.SearchQuery.ToLower())
                    );
                }

                if (request.Params.IsHost && !request.Params.IsGoing) {
                    query = query.Where(x => x.HostUsername == _userAccessor.GetUsername());
                } else if (request.Params.IsGoing) {
                    query = query.Where(x => x.Attendees.Any(a => a.Username == _userAccessor.GetUsername()));
                }

                return Result<PagedList<EventDto>>.Success(
                    await PagedList<EventDto>.CreateAsync(query, request.Params.PageNumber, request.Params.PageSize)
                );
            }
        }
    }
}