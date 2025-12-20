using Core.Enums;

namespace Web.Services
{
    public interface IGameStateService
    {
        GameState Current { get; }
        IDisposable Subscribe(Action<GameState> handler);

        // Comandi della regia
        void SetState(GameState newState);
    }


    public sealed class GameStateService : IGameStateService
    {
        private readonly object _gate = new();
        private GameState _state = new(
            PartitaId: 0,
            MancheId: null,
            MancheDomandaId: null,
            QuestionIndex: null,
            Phase: GamePhase.Idle,
            PhaseEndsAtUtc: null,
            Version: 0);

        private event Action<GameState>? Changed;

        public GameState Current
        {
            get { lock (_gate) return _state; }
        }

        public IDisposable Subscribe(Action<GameState> handler)
        {
            Changed += handler;

            // push immediato allo subscribe
            handler(Current);

            return new Unsubscriber(() => Changed -= handler);
        }

        public void SetState(GameState newState)
        {
            GameState snapshot;
            lock (_gate)
            {
                // Version monotona => facilita debug
                var next = newState with { Version = _state.Version + 1 };
                _state = next;
                snapshot = next;
            }

            Changed?.Invoke(snapshot);
        }

        private sealed class Unsubscriber : IDisposable
        {
            private Action? _dispose;
            public Unsubscriber(Action dispose) => _dispose = dispose;
            public void Dispose() => Interlocked.Exchange(ref _dispose, null)?.Invoke();
        }
    }


}
