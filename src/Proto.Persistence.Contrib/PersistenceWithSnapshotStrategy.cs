using System;
using System.Threading.Tasks;

namespace Proto.Persistence.Contrib
{
    public class PersistenceWithSnapshotStrategy
    {
        private readonly Persistence _persistence;
        private readonly ISnapshotStrategy _snapshotStrategy;
        private readonly Func<object> _getState;

        public PersistenceWithSnapshotStrategy(IProvider provider, string actorId, Action<Event> applyEvent,
            Action<Snapshot> applySnapshot, ISnapshotStrategy snapshotStrategy, Func<object> getState)
        {
            _persistence = Persistence.WithEventSourcingAndSnapshotting(provider, actorId, applyEvent, applySnapshot);
            _snapshotStrategy = snapshotStrategy;
            _getState = getState;
        }

        public Task RecoverStateAsync()
        {
            return _persistence.RecoverStateAsync();
        }

        public async Task PersistEventAsync(object @event)
        {
            await _persistence.PersistEventAsync(@event);
            if (_snapshotStrategy.ShouldTakeSnapshot(new PersistedEvent(@event, _persistence.Index)))
            {
                await PersistSnapshotAsync(_getState());
            }
        }

        public Task PersistSnapshotAsync(object snapshot)
        {
            return _persistence.PersistSnapshotAsync(snapshot);
        }

        public Task DeleteSnapshotsAsync(long inclusiveToIndex)
        {
            return _persistence.DeleteSnapshotsAsync(inclusiveToIndex);
        }

        public Task DeleteEventsAsync(long inclusiveToIndex)
        {
            return _persistence.DeleteEventsAsync(inclusiveToIndex);
        }
    }
}
