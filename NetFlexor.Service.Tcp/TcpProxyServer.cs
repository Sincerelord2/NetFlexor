/*
 * 
 * Author: Sincerelord2
 * 
 * Modifying this code is not allowed without the permission of the author.
 * 
 * License: MIT License
 * 
 * Description:
 *      TCP proxy server for forwarding requests to a remote server.
 *      Only allows connections from specified IP addresses.
 *      SINGLE CONNECTION ONLY.
 * 
 */

using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using NetFlexor.ServiceHelpers;

namespace NetFlexor.Service.Tcp
{
    internal class ActiveRemoteConnection
    {
        public TcpClient Client { get; set; }
        public DateTime LastActivity { get; set; }
    }
    internal class TcpProxyServer
    {
        // TODO: Add a way to handle multiple tcp connections with the same service

        private readonly IPAddress _localIpAddress;
        private readonly int _localPort;
        private readonly IPAddress _forwardIpAddress;
        private readonly int _forwardPort;
        private ActiveRemoteConnection _activeConnection;
        private long _keepAliveSeconds;

        CancellationTokenSource _cts;
        CancellationToken internalToken;

        //private readonly ConcurrentDictionary<TcpClient, DateTime> _activeConnections = new ConcurrentDictionary<TcpClient, DateTime>();

        private readonly string[] _allowedIpAddresses;
        ILogger _logger;
        public TcpProxyServer(IPAddress localIpAddress, int localPort, IPAddress forwardIpAddress, 
            int forwardPort, ILogger logger, string[] allowedIpAddresses, long keepAliveSeconds)
        {
            _localIpAddress = localIpAddress;
            _localPort = localPort;
            _forwardIpAddress = forwardIpAddress;
            _forwardPort = forwardPort;
            _allowedIpAddresses = allowedIpAddresses;
            _keepAliveSeconds = keepAliveSeconds;

            _logger = logger;

            _cts = new CancellationTokenSource();
            internalToken = _cts.Token;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var listener = new TcpListener(_localIpAddress, _localPort);
            listener.Start();
            _logger.LogInformation($"Listening on {_localIpAddress}:{_localPort}...");

            Task.Run(() => CheckConnectionStatus(cancellationToken), cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(cancellationToken);
                var clientEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;
                var clientIp = clientEndPoint.Address;

                if (IpAddressHelpers.IsSourceIpAddressAllowed(_allowedIpAddresses, clientIp.ToString()))
                {
                    _logger.LogInformation($"Client connected from allowed IP [{clientIp.ToString()}]. Starting to forward requests...");
                    Task.Run(() => HandleClientAsync(client, cancellationToken));
                }
                else
                {
                    _logger.LogWarning($"Connection attempt from disallowed IP: {clientIp}. Closing connection.");
                    client.Close();
                }
            }
        }

        private async Task CheckConnectionStatus(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (internalToken.IsCancellationRequested)
                {
                    _cts = new CancellationTokenSource();
                    internalToken = _cts.Token;
                }

                if (_activeConnection is not null)
                {
                    if (DateTime.Now - _activeConnection.LastActivity > TimeSpan.FromSeconds(_keepAliveSeconds) &&
                        _activeConnection.Client.Connected)
                    {
                        _logger.LogWarning("Closing idle connection...");
                        _activeConnection.Client.GetStream().Close();
                        _activeConnection.Client.Close();
                        //await CloseConnectionGracefully(_activeConnection.Client);
                        _cts.Cancel();
                    }
                }

                await Task.Delay(10, cancellationToken);
            }
        }

        private async Task CloseConnectionGracefully(TcpClient targetServer)
        {
            try
            {
                // Finish sending any pending data
                targetServer.Client.Shutdown(SocketShutdown.Send);

                // Receive any remaining data (there shouldn't be any in this case)
                var buffer = new byte[4096];
                while (await targetServer.GetStream().ReadAsync(buffer, 0, buffer.Length) > 0)
                {
                    // Optionally process received data
                }

                // Close the connection
                targetServer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during graceful close: {ex.Message}");
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            using (client)
            {
                var targetServer = await GetForwardTcpClientAsync(cancellationToken);
                
                using (targetServer)
                {
                    var clientStream = client.GetStream();
                    var targetStream = targetServer.GetStream();

                    var clientToServerTask = ForwardDataAsync(clientStream, targetStream, client);
                    var serverToClientTask = ForwardDataAsync(targetStream, clientStream, client);

                    await Task.WhenAll(clientToServerTask, serverToClientTask);
                }
            }
        }

        private async Task ForwardDataAsync(NetworkStream sourceStream, NetworkStream destinationStream, TcpClient client)/*, Action updateLastActivityTime*//*, System.Timers.Timer timer*/
        {
            var buffer = new byte[4096];
            int bytesRead;
            try
            {
                while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, internalToken)) != 0)
                {
                    await destinationStream.WriteAsync(buffer, 0, bytesRead, internalToken);
                    //_logger.LogInformation($"Forwarding {bytesRead} bytes...");
                    _activeConnection.LastActivity = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                if (!internalToken.IsCancellationRequested)
                    _logger.LogError($"Error forwarding data: {ex.Message}");
            }
        }

        private async Task<TcpClient> GetForwardTcpClientAsync(CancellationToken cancellationToken)
        {
            if (_activeConnection is null)
            {
                _activeConnection = new ActiveRemoteConnection{
                    Client = new TcpClient(),
                    LastActivity = DateTime.Now
                };
                
                await _activeConnection.Client.ConnectAsync(_forwardIpAddress, _forwardPort, cancellationToken);
                return _activeConnection.Client;
            }
            else
            {
                if (DateTime.Now - _activeConnection.LastActivity > TimeSpan.FromSeconds(_keepAliveSeconds))
                {
                    if (_activeConnection.Client.Connected)
                        _activeConnection.Client.Close();

                    _activeConnection.Client = new TcpClient();
                    await _activeConnection.Client.ConnectAsync(_forwardIpAddress, _forwardPort, cancellationToken);
                    _activeConnection.LastActivity = DateTime.Now;

                    return _activeConnection.Client;
                }
                else
                {
                    _activeConnection.LastActivity = DateTime.Now;
                    return _activeConnection.Client;
                }
            }
        }

        //private async Task ForwardDataAsync(NetworkStream sourceStream, NetworkStream destinationStream, TcpClient client)
        //{
        //    var buffer = new byte[4096];
        //    int bytesRead;
        //    try
        //    {
        //        while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
        //        {
        //            await destinationStream.WriteAsync(buffer, 0, bytesRead);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"Error forwarding data: {ex.Message}");
        //    }
        //}

        //private void CheckAndCloseIdleConnections(/*TcpClient currentClient*/)
        //{
        //    //foreach (var connection in _activeConnections)
        //    //{
        //    //    if (DateTime.Now - connection.Value > TimeSpan.FromSeconds(10))
        //    //    {
        //    //        connection.Key.Close();
        //    //        _activeConnections.TryRemove(connection.Key, out _);
        //    //    }
        //    //}
        //}
    }
}
