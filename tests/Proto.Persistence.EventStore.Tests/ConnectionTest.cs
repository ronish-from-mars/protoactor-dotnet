using System;
using Xunit;
using EventStore.ClientAPI;
namespace Proto.Persistence.EventStore.Tests
{
    [Collection("EventStoreTests")]
    public class ConnectionTest
    {
        readonly EventStoreProcess _eventStoreProcess;

        public ConnectionTest(EventStoreProcess eventStoreProcess)
        {
            _eventStoreProcess = eventStoreProcess;
        }

        [Fact]
        public async void TrueDat()
        {
            var eventStoreProvider = new EventStoreProviderState(_eventStoreProcess.InMemoryEventStore);
            var actorName = "test-" + Guid.NewGuid();
            await eventStoreProvider.PersistEventAsync(actorName, 0, "whatever");
            await eventStoreProvider.GetEventsAsync(actorName, 0, o =>
            {
                Assert.Equal("whatever", o.ToString());
            });
        }
    }
}
