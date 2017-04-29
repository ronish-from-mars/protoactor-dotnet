using Proto.Persistence.Contrib.SnapshotStrategies;
using Xunit;

namespace Proto.Persistence.Contrib.Tests.SnapshotStrategies
{
    public class EventTypeStrategyTests
    {
        [Fact]
        public void EventTypeStrategy_ShouldSnapshotAccordingToTheEventType()
        {
            var strategy = new EventTypeStrategy(typeof(int));
            Assert.True(strategy.ShouldTakeSnapshot(new PersistedEvent(1, 0)));
            Assert.False(strategy.ShouldTakeSnapshot(new PersistedEvent("not an int", 0)));
        }
    }
}