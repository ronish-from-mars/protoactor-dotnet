using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace EventStorePersistence
{
    public class EventStoreConnectionFactory
    {
        private UserCredentials _userCredentials;
        private string _ipAddress;
        private int _tcpPort = 1113;
        private int _httpPort = 2113;
        private string _clusterDns;
        private ILogger _logger;
        private string _connectionName;
        private Action<ConnectionSettingsBuilder> _fineTuning;
        private EventHandler<ClientConnectionEventArgs> _onEventStoreConnected;

        public interface ISettings
        {
            string Username { get; }
            string Password { get; }
            string IpAddress { get; }
            int? TcpPort { get; }
            int? HttpPort { get; }
            string ClusterDns { get; }
        }

        public class AppSettings : ISettings
        {
            private readonly NameValueCollection _appSettings;

            public AppSettings(NameValueCollection appSettings)
            {
                _appSettings = appSettings;
            }

            public string Username
            {
                get { return _appSettings["eventstore:Username"]; }
            }

            public string Password
            {
                get { return _appSettings["eventstore:Password"]; }
            }
            public string IpAddress
            {
                get { return _appSettings["eventstore:IpAddress"]; }
            }
            public int? TcpPort
            {
                get
                {
                    var port = _appSettings["eventstore:TcpPort"];
                    return string.IsNullOrEmpty(port) ? (int?)null : int.Parse(port);
                }
            }
            public int? HttpPort
            {
                get
                {
                    var port = _appSettings["eventstore:HttpPort"];
                    return string.IsNullOrEmpty(port) ? (int?)null : int.Parse(port);
                }
            }
            public string ClusterDns
            {
                get { return _appSettings["eventstore:ClusterDns"]; }
            }
        }

        public static EventStoreConnectionFactory Create()
        {
            return new EventStoreConnectionFactory();
        }

        public EventStoreConnectionFactory WithEventOnConnection(EventHandler<ClientConnectionEventArgs> onEventStoreConnected)
        {
            _onEventStoreConnected = onEventStoreConnected;
            return this;
        }

        public EventStoreConnectionFactory WithCredentials(string username, string password)
        {
            _userCredentials = new UserCredentials(username, password);
            return this;
        }

        public EventStoreConnectionFactory WithCredentials(UserCredentials userCredentials)
        {
            _userCredentials = userCredentials;
            return this;
        }

        public EventStoreConnectionFactory WithLogger(ILogger logger)
        {
            _logger = logger;
            return this;
        }

        public EventStoreConnectionFactory WithConnectionName(string connectionName)
        {
            _connectionName = connectionName;
            return this;
        }

        public EventStoreConnectionFactory WithFineTuning(Action<ConnectionSettingsBuilder> fineTuning)
        {
            _fineTuning = fineTuning;
            return this;
        }

        private void WithIpAddress(string ipAddress)
        {
            _ipAddress = ipAddress;
        }

        private void WithTcpPort(int tcpPort)
        {
            _tcpPort = tcpPort;
        }

        private void WithHttpPort(int httpPort)
        {
            _httpPort = httpPort;
        }

        private void WithClusterDns(string clusterDns)
        {
            _clusterDns = clusterDns;
        }

        private IEventStoreConnection Connect(Func<ConnectionSettingsBuilder, IEventStoreConnection> createConnection)
        {
            var connection = BuildEventStoreConnection(createConnection);

            connection.ConnectAsync().Wait();

            return connection;
        }

        private IEventStoreConnection BuildEventStoreConnection(Func<ConnectionSettingsBuilder, IEventStoreConnection> createConnection)
        {
            var connectionSettingsBuilder = ConnectionSettings.Create();

            if (_userCredentials != null)
                connectionSettingsBuilder.SetDefaultUserCredentials(_userCredentials);

            if (_logger != null)
                connectionSettingsBuilder.UseCustomLogger(_logger);

            connectionSettingsBuilder
                .SetHeartbeatTimeout(TimeSpan.FromMilliseconds(3000))
                .SetHeartbeatInterval(TimeSpan.FromMilliseconds(1500))
                .SetReconnectionDelayTo(TimeSpan.FromSeconds(5))
                .WithConnectionTimeoutOf(TimeSpan.FromSeconds(10))
                .SetOperationTimeoutTo(TimeSpan.FromSeconds(15))
                .KeepReconnecting();

            if (_fineTuning != null)
                _fineTuning(connectionSettingsBuilder);

            var connection = createConnection(connectionSettingsBuilder);

            if (_onEventStoreConnected != null)
            {
                connection.Connected += _onEventStoreConnected;
            }

            return connection;
        }

        public IEventStoreConnection ConnectToCluster(string clusterDns, int clusterGossipPort)
        {
            return Connect(connectionSettings =>
            {
                var clusterSettings = ClusterSettings.Create()
                    .DiscoverClusterViaDns()
                    .SetClusterDns(clusterDns)
                    .SetClusterGossipPort(clusterGossipPort);
                return EventStoreConnection.Create(connectionSettings, clusterSettings, _connectionName);
            });
        }

        public IEventStoreConnection BuildDisconnectedClusterConnection(string clusterDns, int clusterGossipPort)
        {
            return BuildEventStoreConnection(connectionSettings =>
            {
                var clusterSettings = ClusterSettings.Create()
                    .DiscoverClusterViaDns()
                    .SetClusterDns(clusterDns)
                    .SetClusterGossipPort(clusterGossipPort);
                return EventStoreConnection.Create(connectionSettings, clusterSettings, _connectionName);
            });
        }

        public IEventStoreConnection ConnectToSingleNode(string ipAddress, int tcpPort)
        {
            return Connect(connectionSettings =>
            {
                var tcpEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), tcpPort);
                return EventStoreConnection.Create(connectionSettings, tcpEndPoint, _connectionName);
            });
        }

        public IEventStoreConnection BuildDisconnectedSingleNodeConnection(string ipAddress, int tcpPort)
        {
            return BuildEventStoreConnection(connectionSettings =>
            {
                var tcpEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), tcpPort);
                return EventStoreConnection.Create(connectionSettings, tcpEndPoint, _connectionName);
            });
        }

        public IEventStoreConnection ConnectWithSettings(ISettings settings)
        {
            if (!string.IsNullOrEmpty(settings.Username) && !string.IsNullOrEmpty(settings.Password))
                WithCredentials(settings.Username, settings.Password);

            if (!string.IsNullOrEmpty(settings.IpAddress))
                WithIpAddress(settings.IpAddress);

            if (settings.TcpPort.HasValue)
                WithTcpPort(settings.TcpPort.Value);

            if (settings.HttpPort.HasValue)
                WithHttpPort(settings.HttpPort.Value);

            if (!string.IsNullOrEmpty(settings.ClusterDns))
                WithClusterDns(settings.ClusterDns);

            return IsSingleNodeConnection()
                ? ConnectToSingleNode(_ipAddress, _tcpPort)
                : ConnectToCluster(_clusterDns, _httpPort);
        }

        public IEventStoreConnection ConnectWithAppSettings(NameValueCollection appSettings)
        {
            return ConnectWithSettings(new AppSettings(appSettings));
        }

        private bool IsSingleNodeConnection()
        {
            return string.IsNullOrEmpty(_clusterDns);
        }
    }
}
