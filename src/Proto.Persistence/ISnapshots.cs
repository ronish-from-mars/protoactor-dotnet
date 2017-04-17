// -----------------------------------------------------------------------
//  <copyright file="ISnapshots.cs" company="Asynkron HB">
//      Copyright (C) 2015-2017 Asynkron HB All rights reserved
//  </copyright>
// -----------------------------------------------------------------------

namespace Proto.Persistence
{
    public interface ISnapshots : IActor
    {
        Snapshots Snapshots { get; set; }
        void Apply(Snapshot snapshot);
    }

    public interface IEventSourced : IActor
    {
        Events Events { get; set; }
        void Apply(Event @event);
    }
}