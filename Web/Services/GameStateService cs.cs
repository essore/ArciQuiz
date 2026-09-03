using Core.Enums;

namespace Web.Services
{
    public interface IGameStateService
    {
        GameState Current { get; }
        IDisposable Subscribe(Action<GameState> handler);

        // Comandi della regia
        void SetState(GameState newState);
        void SetQrVisibility(int partitaId, bool isQrVisible);
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
            SecondsRemainingHint: null,
            AcceptingAnswers: false,
            PublicMessage: null,
            Version: 0,
            IsQrVisible: true);

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
                var next = newState with
                {
                    IsQrVisible = newState.PartitaId == _state.PartitaId
                        ? _state.IsQrVisible
                        : true,
                    Version = _state.Version + 1
                };
                _state = next;
                snapshot = next;
            }

            Changed?.Invoke(snapshot);
        }

        public void SetQrVisibility(int partitaId, bool isQrVisible)
        {
            GameState snapshot;
            lock (_gate)
            {
                if (_state.PartitaId != partitaId)
                    return;

                snapshot = _state with
                {
                    IsQrVisible = isQrVisible,
                    Version = _state.Version + 1
                };
                _state = snapshot;
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
