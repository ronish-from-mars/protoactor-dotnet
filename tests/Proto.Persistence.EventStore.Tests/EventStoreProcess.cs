using System;
using System.Net;
using System.Threading;
using EventStore.ClientAPI;
using Xunit;

namespace Proto.Persistence.EventStore.Tests
{
    [CollectionDefinition("EventStoreTests")]
    public class EventStoreCollection : ICollectionFixture<EventStoreProcess>
    {
    }

    public class EventStoreProcess : IDisposable
    {
        private static string EventStorePath => Environment.GetEnvironmentVariable("EventstoreHome") ??
                                                @"C:\EventStore 4.0.0\EventStore.ClusterNode.exe";

        private readonly System.Diagnostics.Process _process;
        public IEventStoreConnection InMemoryEventStore { get; private set; }
        const int tcpPort = 1501;
        const int httpPort = 2501;

        public EventStoreProcess()
        {
            _process = new System.Diagnostics.Process
            {
                StartInfo =
                {
                    Arguments =
                        string.Format(
                            "--mem-db " +
                            "--ext-tcp-port={0} --ext-http-port={1} --ext-http-prefixes=\"{2}\"",
                            tcpPort,
                            httpPort,
                            @"http://127.0.0.1:2501/"),
                    CreateNoWindow = false,
                    UseShellExecute = false,
                    FileName = EventStorePath
                }
            };
            _process.Start();

            Thread.Sleep(2500); // wait for ES to initialise
            InMemoryEventStore = EventStoreConnection.Create(new IPEndPoint(IPAddress.Loopback, 1501));
            
            InMemoryEventStore.ConnectAsync().Wait();
        }

        public void Dispose()
        {
            if (InMemoryEventStore != null)
            {
                InMemoryEventStore.Close();
                InMemoryEventStore.Dispose();
                InMemoryEventStore = null;
            }
            if (_process == null)
                return;

            if (_process.HasExited)
                return;

            _process.Kill();
        }
    }
}
