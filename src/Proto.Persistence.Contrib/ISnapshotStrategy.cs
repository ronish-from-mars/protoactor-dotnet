namespace Proto.Persistence.Contrib
{
    public interface ISnapshotStrategy
    {
        bool ShouldTakeSnapshot(PersistedEvent persistedEvent);
    }
}