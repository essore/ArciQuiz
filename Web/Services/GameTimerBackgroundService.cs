using Infrasctructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Web.Services;

public sealed class GameTimerBackgroundService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);
    private readonly IDbContextFactory<ArciQuizDbContext> _databaseFactory;
    private readonly IGameStateService _gameState;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<GameTimerBackgroundService> _logger;

    public GameTimerBackgroundService(
        IDbContextFactory<ArciQuizDbContext> databaseFactory,
        IGameStateService gameState,
        TimeProvider timeProvider,
        ILogger<GameTimerBackgroundService> logger)
    {
        _databaseFactory = databaseFactory;
        _gameState = gameState;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    // Controlla subito lo stato persistito all'avvio e continua a chiudere le domande scadute.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var database = await _databaseFactory.CreateDbContextAsync(stoppingToken);
                var changedPartitaIds = await GameTimerService.ChiudiDomandeScaduteAsync(
                    database,
                    _timeProvider,
                    stoppingToken);

                foreach (var partitaId in changedPartitaIds)
                {
                    var state = await PartitaStateMachineService.CaricaStatoAsync(
                        database,
                        partitaId,
                        _timeProvider,
                        stoppingToken);
                    if (state is not null)
                        _gameState.SetState(state);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Errore durante il controllo delle scadenze delle domande.");
            }

            try
            {
                await Task.Delay(PollInterval, _timeProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
