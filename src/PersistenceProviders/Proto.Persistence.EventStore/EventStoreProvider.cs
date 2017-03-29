using EventStore.ClientAPI;

namespace Proto.Persistence.EventStore
{
    public class EventStoreProvider : IProvider
    {
        private readonly IEventStoreConnection _eventStoreConnection;

        public EventStoreProvider(IEventStoreConnection eventStoreConnection)
        {
            _eventStoreConnection = eventStoreConnection;
        }

        public IProviderState GetState()
        {
            return new EventStoreProviderState(_eventStoreConnection);
        }
    }
}
