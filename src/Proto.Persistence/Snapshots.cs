// -----------------------------------------------------------------------
//  <copyright file="Snapshots.cs" company="Asynkron HB">
//      Copyright (C) 2015-2017 Asynkron HB All rights reserved
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading.Tasks;

namespace Proto.Persistence
{
    public class Events
    {
        private IProviderState _state;
        private IEventSourced _actor;
        public long Index { get; private set; }
        private string _actorId;
        
        public async Task InitAsync(IProvider provider, string actorId, IEventSourced actor, long index = 0)
        {
            Index = index;
            _state = provider.GetState();
            _actor = actor;
            _actorId = actorId;
            await _state.GetEventsAsync(_actorId, Index, @event =>
            {
                Index++;
                _actor.Apply(new RecoverEvent(@event, Index));
            });
        }

        public async Task PersistEventAsync(object @event)
        {
            Index++;
            var index = Index;
            await _state.PersistEventAsync(_actorId, index, @event);
            _actor.Apply(new PersistedEvent(@event, index));
        }

        public async Task DeleteEventsAsync(long inclusiveToIndex)
        {
            await _state.DeleteEventsAsync(_actorId, inclusiveToIndex);
        }

        public static Func<Receive, Receive> Using(IProvider provider)
        {
            return next => async (context) =>
            {
                switch (context.Message)
                {
                    case Started _:
                        if (context.Actor is IEventSourced actor)
                        {
                            actor.Events = new Events();
                            await actor.Events.InitAsync(provider, context.Self.Id, actor);
                        }
                        break;
                }

                await next(context);
            };
        }

    }

    public class Snapshots
    {
        private IProviderState _state;
        private ISnapshots _actor;
        public long Index { get; private set; }
        private IContext _context;
        private string ActorId => _context.Self.Id;

        public async Task InitAsync(IProvider provider, IContext context, ISnapshots actor)
        {
            _state = provider.GetState();
            _context = context;
            _actor = actor;

            var (snapshot, index) = await _state.GetSnapshotAsync(ActorId);

            if (snapshot != null)
            {
                Index = index;
                _actor.Apply(new RecoverSnapshot(snapshot, index));
            };
        }

        public async Task PersistSnapshotAsync(object snapshot, long index)
        {
            await _state.PersistSnapshotAsync(ActorId, index, snapshot);
        }

        public async Task DeleteSnapshotsAsync(long inclusiveToIndex)
        {
            await _state.DeleteSnapshotsAsync(ActorId, inclusiveToIndex);
        }

        public static Func<Receive, Receive> Using(IProvider provider)
        {
            return next => async context =>
            {
                switch (context.Message)
                {
                    case Started _:
                        if (context.Actor is ISnapshots actor)
                        {
                            actor.Snapshots = new Snapshots();
                            await actor.Snapshots.InitAsync(provider, context, actor);
                            
                        }
                        break;
                }
                
                await next(context);
            };
        }
    }

    public class RequestSnapshot { }

    public class Snapshot
    {
        public object State { get; }
        public long Index { get; }

        public Snapshot(object state, long index)
        {
            State = state;
            Index = index;
        }
    }
    public class RecoverSnapshot : Snapshot
    {
        public RecoverSnapshot(object state, long index) : base(state, index)
        {
        }
    }

    public class PersistedSnapshot : Snapshot
    {
        public PersistedSnapshot(object state, long index) : base(state, index)
        {
        }
    }

    public class Event
    {
        public object Data { get; }
        public long Index { get; }

        public Event(object data, long index)
        {
            Data = data;
            Index = index;
        }
    }
    public class RecoverEvent : Event
    {
        public RecoverEvent(object data, long index) : base(data, index)
        {
        }
    }

    public class PersistedEvent : Event
    {
        public PersistedEvent(object data, long index) : base(data, index)
        {
        }
    }
}