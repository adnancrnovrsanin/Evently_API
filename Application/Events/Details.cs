using Application.Core;
using Application.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Events
{
    public class Details
    {
        public class Query : IRequest<Result<EventDto>> {
            public Guid Id { get; set; }
        }

        public class Handler : IRequestHandler<Query, Result<EventDto>>
        {
            private readonly DataContext _context;
            private readonly IMapper _mapper;
            private readonly IUserAccessor _userAccessor;
            public Handler(DataContext context, IMapper mapper, IUserAccessor userAccessor)
            {
                _userAccessor = userAccessor;
                _mapper = mapper;
                _context = context;
            }
            public async Task<Result<EventDto>> Handle(Query request, CancellationToken cancellationToken)
            {
                var newEvent = await _context.Events
                    .ProjectTo<EventDto>(_mapper.ConfigurationProvider, new { currentUsername = _userAccessor.GetUsername() })
                    .FirstOrDefaultAsync(x => x.Id == request.Id);

                return Result<EventDto>.Success(newEvent);
            }
        }
    }
}