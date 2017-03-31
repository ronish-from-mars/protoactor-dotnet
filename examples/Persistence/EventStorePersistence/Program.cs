using System;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;
using EventStore.ClientAPI.SystemData;
using Messages;
using Proto;
using Proto.Persistence;
using Proto.Persistence.EventStore;

namespace EventStorePersistence
{
    class Program
    {
        public static IEventStoreConnection ConnectToEventStore(string username, string password, string ipAddress, int tcpPort)
        {
            return EventStoreConnectionFactory.Create()
                .WithCredentials(new UserCredentials(username, password))
                .WithLogger(new ConsoleLogger())
                .BuildDisconnectedSingleNodeConnection(ipAddress, tcpPort);
        }

        static void Main(string[] args)
        {
            var eventStoreConnection = ConnectToEventStore("admin", "changeit", "127.0.0.1", 1113);
            eventStoreConnection.ConnectAsync().Wait();
            
            var provider = new EventStoreProvider(eventStoreConnection);
            var props = Actor.FromProducer(() => new MyPersistenceActor())
                .WithReceiveMiddleware(Persistence.Using(provider));
            var pid = Actor.SpawnNamed(props, "test-actor2");
           // pid.Tell(new RenameCommand { Name = "Christian" });
//            pid.Tell(new RenameCommand { Name = "Alex" });
//            pid.Tell(new RenameCommand { Name = "Roger" });
//            Console.WriteLine("Hello World!");
            Console.ReadLine();
        }

        class MyPersistenceActor : IPersistentActor
        {
            private PID _loopActor;
            private State _state = new State();
            public Persistence Persistence { get; set; }
            private class StartLoopActor { }
            private class TimeToSnapshot { }

            private bool _timerStarted = false;

            public async Task ReceiveAsync(IContext context)
            {
                switch (context.Message)
                {
                    case Started msg:
                        Console.WriteLine("MyPersistenceActor - Started");
                        context.Self.Tell(new StartLoopActor());
                        break;
                    case RecoveryStarted msg:
                        Console.WriteLine("MyPersistenceActor - RecoveryStarted");
                        break;
                    case RecoveryCompleted msg:
                        Console.WriteLine("MyPersistenceActor - RecoveryCompleted");
                        context.Self.Tell(new StartLoopActor());
                        break;
                    case RecoverSnapshot msg:
                        if (msg.Data is State ss)
                        {
                            _state = ss;
                            Console.WriteLine("MyPersistenceActor - RecoverSnapshot = {0}, Snapshot.Name = {1}", Persistence.SnapshotIndex, ss.Name);
                        }
                        break;
                    case RecoverEvent msg:
                        if (msg.Data is RenameEvent recev)
                        {
                            Console.WriteLine("MyPersistenceActor - RecoverEvent = {0}, Event.Name = {1}", Persistence.EventIndex, recev.Name);
                        }
                        break;
                    case PersistedSnapshot msg:
                        await Handle(msg);
                        break;
                    case PersistedEvent msg:
                        Console.WriteLine("MyPersistenceActor - PersistedEvent = {0}", msg.Index);
                        if (msg.Data is RenameEvent rne)
                        {
                            _state.Name = rne.Name;
                        }
                        break;
                    case RequestSnapshot msg:
                        await Handle(context, msg);
                        break;
                    case TimeToSnapshot msg:
                        await Handle(context, msg);
                        break;
                    case StartLoopActor msg:
                        await Handle(context, msg);
                        break;
                    case RenameCommand msg:
                        await Handle(msg);
                        break;
                }
            }

            private Task Handle(PersistedSnapshot message)
            {
                Console.WriteLine("MyPersistenceActor - PersistedSnapshot");
                var sn_index = Persistence.SnapshotIndex - 2;
                //await Persistence.State.DeleteSnapshotsAsync(Persistence.Name, sn_index);
                var ev_index = Persistence.EventIndex;
                //await Persistence.State.DeleteEventsAsync(Persistence.Name, ev_index);
                return Actor.Done;
            }

            private async Task Handle(IContext context, RequestSnapshot message)
            {
                Console.WriteLine("MyPersistenceActor - RequestSnapshot");
                await Persistence.PersistSnapshotAsync(_state);
                context.Self.Tell(new TimeToSnapshot());
            }

            private async Task Handle(IContext context, TimeToSnapshot message)
            {
                Console.WriteLine("MyPersistenceActor - TimeToSnapshot");
                await Task.Delay(TimeSpan.FromSeconds(10));
                context.Self.Tell(new RequestSnapshot());
            }

            private Task Handle(IContext context, StartLoopActor message)
            {
                if (_timerStarted) return Actor.Done;
                _timerStarted = true;
                Console.WriteLine("MyPersistenceActor - StartLoopActor");
                var props = Actor.FromProducer(() => new LoopActor(Persistence.EventIndex));
                _loopActor = context.Spawn(props);
                context.Self.Tell(new TimeToSnapshot());
                return Actor.Done;
            }

            private async Task Handle(RenameCommand message)
            {
                Console.WriteLine("MyPersistenceActor - RenameCommand");
                await Persistence.PersistReceiveAsync(new RenameEvent { Name = message.Name });
            }
        }

        class LoopActor : IActor
        {
            internal class LoopParentMessage { }
            long prefixCounter = 0;

            public LoopActor(long startPrefixCounter)
            {
                prefixCounter = startPrefixCounter;
            }

            public Task ReceiveAsync(IContext context)
            {
                switch (context.Message)
                {
                    case Started _:
                        Console.WriteLine("LoopActor - Started");
                        context.Self.Tell(new LoopParentMessage());
                        break;
                    case LoopParentMessage msg:
                        Task.Run(async () => {
                            context.Parent.Tell(new RenameCommand { Name = "Daniel-" + prefixCounter });
                            prefixCounter++;
                            await Task.Delay(TimeSpan.FromSeconds(2));
                            context.Self.Tell(new LoopParentMessage());
                        });
                        break;
                }
                return Actor.Done;
            }
        }
    }
}