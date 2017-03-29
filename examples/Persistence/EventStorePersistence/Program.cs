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
            var props = Actor.FromProducer(() => new MyActor())
                .WithReceiveMiddleware(Persistence.Using(provider));
            var pid = Actor.SpawnNamed(props, "test-actor");
            pid.Tell(new RenameCommand { Name = "Christian" });
            pid.Tell(new RenameCommand { Name = "Alex" });
            pid.Tell(new RenameCommand { Name = "Roger" });
            Console.WriteLine("Hello World!");
            Console.ReadLine();
        }

        class MyActor : IPersistentActor
        {
            private State _state = new State();
            public Persistence Persistence { get; set; }

            public async Task ReceiveAsync(IContext context)
            {
                switch (context.Message)
                {
                    case RenameCommand rc:
                        await Persistence.PersistReceiveAsync(new RenameEvent { Name = rc.Name });
                        break;
                    case RenameEvent re:
                        _state.Name = re.Name;
                        Console.WriteLine($"{context.Self.Id} changed name to {_state.Name}");
                        break;
                    case RequestSnapshot rs:
                        await Persistence.PersistSnapshotAsync(_state);
                        break;
                    case State s:
                        _state = s;
                        break;
                }
            }
        }
    }
}