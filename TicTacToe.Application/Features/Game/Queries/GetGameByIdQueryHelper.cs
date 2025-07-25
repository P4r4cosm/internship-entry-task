using AutoMapper;
using MediatR;
using TicTacToe.Application.Common.Exceptions;
using TicTacToe.Application.DTOs;
using TicTacToe.Application.Interfaces;

namespace TicTacToe.Application.Features.Game.Queries;

public class GetGameByIdQueryHandler : IRequestHandler<GetGameByIdQuery, GameDto>
{
    private readonly IGameRepository _gameRepository;
    private readonly IMapper _mapper;

    public GetGameByIdQueryHandler(IGameRepository gameRepository, IMapper mapper)
    {
        _gameRepository = gameRepository;
        _mapper = mapper;
    }

    public async Task<GameDto> Handle(GetGameByIdQuery request, CancellationToken cancellationToken)
    {
        var game = await _gameRepository.GetByIdAsync(request.Id, cancellationToken);

        if (game == null)
        {
            // Бросаем кастомное исключение, которое API слой сможет перехватить
            throw new NotFoundException(nameof(Domain.Entities.Game), request.Id);
        }
        return _mapper.Map<GameDto>(game);
    }
}