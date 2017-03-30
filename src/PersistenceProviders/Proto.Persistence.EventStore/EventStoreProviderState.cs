using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Proto.Persistence.EventStore
{
    public class EventStoreProviderState : IProviderState
    {
        private const string SnapshotIndexKey = "snapshotIndexedAtEventNumber";
        private const string TypeInfoKey = "eventClrTypeName";
        private readonly IEventStoreConnection _connection;

        public EventStoreProviderState(IEventStoreConnection connection)
        {
            _connection = connection;
        }

        public async Task GetEventsAsync(string actorName, long indexStart, Action<object> callback)
        {
            StreamEventsSlice currentSlice;
            long sliceStart = StreamPosition.Start;
            var events = new List<object>();
            do
            {
                currentSlice = await _connection.ReadStreamEventsForwardAsync(actorName, sliceStart, 500, true);
                sliceStart = currentSlice.NextEventNumber;
                events.AddRange(currentSlice.Events.Select(Deserialize));
            } while (!currentSlice.IsEndOfStream);
            
            foreach (var @event in events)
            {
                callback(@event);
            }
        }

        private object Deserialize(ResolvedEvent @event)
        {
            var typeInfo = GetValueFromMetadata(@event, TypeInfoKey);
            var eventType = Type.GetType((string)typeInfo);
            return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(@event.Event.Data), eventType);
        }

        public async Task<Tuple<object, long>> GetSnapshotAsync(string actorName)
        {
            var currentSlice = await _connection.ReadStreamEventsBackwardAsync(actorName + "-snapshots", StreamPosition.End, 1, true);
            if (currentSlice.Status != SliceReadStatus.Success || 
                currentSlice.Events.Length == 0 || 
                currentSlice.Events[0].OriginalEvent.Metadata.Length == 0) return null;
            var resolvedEvent = currentSlice.Events[0];
            var snapshotIndex = GetValueFromMetadata(resolvedEvent, SnapshotIndexKey);
            var @event = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(resolvedEvent.Event.Data), SerializationSettings.StandardSettings());
            return Tuple.Create(@event, (long)snapshotIndex);
        }

        private JToken GetValueFromMetadata(ResolvedEvent @event, string key)
        {
            var metadata = @event.OriginalEvent.Metadata;
            var metaDataString = Encoding.UTF8.GetString(metadata);
            var metaDataJson = JObject.Parse(metaDataString);
            return metaDataJson.Property(key).Value;
        }

        public async Task PersistEventAsync(string actorName, long index, object @event)
        {
            await SaveEvent(actorName, index, @event, new Dictionary<string, object>
            {
                {TypeInfoKey, @event.GetType().AssemblyQualifiedName}
            });
        }

        public async Task PersistSnapshotAsync(string actorName, long index, object snapshot)
        {
            await SaveEvent(actorName, index, snapshot, new Dictionary<string, object>
            {
                {SnapshotIndexKey, index}
            });
        }

        private async Task SaveEvent(string streamName, long index, object @event, Dictionary<string, object> metaData)
        {
            var jsonString = JsonConvert.SerializeObject(@event, SerializationSettings.StandardSettings());
            var data = Encoding.UTF8.GetBytes(jsonString);
            var metaDataBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(metaData, SerializationSettings.StandardSettings()));
            var eventData = new EventData(Guid.NewGuid(), @event.GetType().Name, true, data, metaDataBytes);
            var expectedVersion = index - 1;
            await _connection.AppendToStreamAsync(streamName, expectedVersion, eventData);
        }

        public async Task DeleteEventsAsync(string actorName, long fromIndex)
        {
            // TODO we could do something like mark all events up until the fromIndex as 
            // $tb(truncate before) on stream metadata? Setting it makes the 
            // stream start from whatever number is there (and makes all earlier
            // events eligible for scavenge) - see https://groups.google.com/forum/#!topic/event-store/yqti9smstoo
            throw new NotSupportedException("EventStore is an immutable database that does not support deletes");
        }

        public async Task DeleteSnapshotsAsync(string actorName, long fromIndex)
        {
            throw new NotSupportedException("EventStore is an immutable database that does not support deletes");
        }
    }
}
