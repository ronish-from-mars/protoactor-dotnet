namespace Proto.Persistence.Contrib.Tests
{
    public partial class PersistenceWithSnapshotStrategiesTests
    {
        public class InMemoryProvider : IProvider
        {
            private readonly IProviderState _state;
            // allow passing in of IProviderState to allow state to "persist" across actor restarts
            public InMemoryProvider(InMemoryProviderState state = null)
            {
                _state = state;
            }
            public IProviderState GetState()
            {
                return _state ?? new InMemoryProviderState();
            }
        }
    }
}