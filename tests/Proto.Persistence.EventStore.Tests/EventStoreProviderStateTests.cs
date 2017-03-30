using System;
using System.Threading.Tasks;
using Xunit;
using EventStore.ClientAPI;
namespace Proto.Persistence.EventStore.Tests
{
    [Collection("EventStoreTests")]
    public class EventStoreProviderStateTests
    {
        readonly EventStoreProcess _eventStoreProcess;

        public EventStoreProviderStateTests(EventStoreProcess eventStoreProcess)
        {
            _eventStoreProcess = eventStoreProcess;
        }

        [Fact]
        public async void can_save_and_read_events()
        {
            var eventStoreProvider = new EventStoreProviderState(_eventStoreProcess.InMemoryEventStore);
            var actorName = Guid.NewGuid().ToString("N");
            await eventStoreProvider.PersistEventAsync(actorName, 0, "whatever");
            await eventStoreProvider.GetEventsAsync(actorName, 0, o =>
            {
                Assert.Equal("whatever", o.ToString());
            });
        }

        [Fact]
        public async void get_events_does_not_call_callback_when_unknown_stream()
        {
            var callbackCalled = false;
            var eventStoreProvider = new EventStoreProviderState(_eventStoreProcess.InMemoryEventStore);
            await eventStoreProvider.GetEventsAsync("this doesnt exist", 0, o =>
            {
                callbackCalled = true;
            });
            Assert.False(callbackCalled);
        }

        [Fact]
        public async void can_save_and_read_snapshots()
        {
            var eventStoreProvider = new EventStoreProviderState(_eventStoreProcess.InMemoryEventStore);
            var actorName = Guid.NewGuid().ToString("N");
            await eventStoreProvider.PersistSnapshotAsync(actorName, 0, "whatever");
            var snapshot = await eventStoreProvider.GetSnapshotAsync(actorName);
            Assert.Equal("whatever", snapshot.Item1);
            Assert.Equal(0, snapshot.Item2);
        }

        [Fact]
        public async void get_snapshot_returns_null_when_unknown_stream()
        {
            var eventStoreProvider = new EventStoreProviderState(_eventStoreProcess.InMemoryEventStore);
            var snapshot = await eventStoreProvider.GetSnapshotAsync("this doesnt exist");
            Assert.Null(snapshot);
        }
    }
}
