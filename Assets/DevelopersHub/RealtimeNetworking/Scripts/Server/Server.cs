using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using DevelopersHub.RealtimeNetworking.Common;

namespace DevelopersHub.RealtimeNetworking.Server
{
    class Server
    {

        public static int maxUsers { get; private set; }
        public static int port { get; private set; }
        public static Dictionary<int, Client> clients = new Dictionary<int, Client>();
        public delegate void PacketHandler(int clientID, Packet packet);
        public static Dictionary<int, PacketHandler> packetHandlers;

        private static TcpListener _tcpListener;
        private static UdpClient _udpListener;

        public static void Start(int maxUsers, int port)
        {
            Server.maxUsers = maxUsers;
            Server.port = port;
            for (int i = 1; i <= Server.maxUsers; i++)
            {
                clients.Add(i, new Client(i));
            }
            packetHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)Packet.ID.INITIALIZATION, Receiver.Initialization },
                { (int)Packet.ID.CUSTOM, Receiver.ReceiveCustom },
                { (int)Packet.ID.INTERNAL, Receiver.ReceiveInternal },
            };
            _tcpListener = new TcpListener(IPAddress.Any, Server.port);
            _tcpListener.Start();
            _tcpListener.BeginAcceptTcpClient(OnConnectedTCP, null);
            if (RealtimeNetworking.udpActive)
            {
                _udpListener = new UdpClient(Server.port);
                _udpListener.BeginReceive(OnConnectedUDP, null);
            }
        }

        public static void Stop()
        {
            for (int i = 1; i <= maxUsers; i++)
            {
                clients[i].tcp?.Disconnect();
                clients[i].udp?.Disconnect();
            }
            clients.Clear();
            
            _tcpListener?.Stop();
            _tcpListener = null;

            _udpListener?.Close();
            _udpListener = null;
        }

        private static void OnConnectedTCP(IAsyncResult result)
        {
            TcpClient client = _tcpListener.EndAcceptTcpClient(result);
            _tcpListener.BeginAcceptTcpClient(OnConnectedTCP, null);
            Console.WriteLine("Incoming connection from {0}.", client.Client.RemoteEndPoint);
            for (int i = 1; i <= maxUsers; i++)
            {
                if (clients[i].tcp.socket == null && clients[i].accountID < 0 && clients[i].disconnecting == false)
                {
                    clients[i].tcp.Initialize(client);
                    IPEndPoint ip = client.Client.RemoteEndPoint as IPEndPoint;
                    RealtimeNetworking._ClientConnected(i, ip.Address.ToString());
                    return;
                }
            }
            Console.WriteLine("{0} failed to connect. Server is at full capacity.", client.Client.RemoteEndPoint);
        }

        private static void OnConnectedUDP(IAsyncResult result)
        {
            try
            {
                IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = _udpListener.EndReceive(result, ref clientEndPoint);
                _udpListener.BeginReceive(OnConnectedUDP, null);
                if (data.Length < 4)
                {
                    return;
                }
                using (Packet packet = new Packet(data))
                {
                    int id = packet.ReadInt();
                    if (id == 0)
                    {
                        return;
                    }
                    if (clients[id].udp.endPoint == null)
                    {
                        clients[id].udp.Connect(clientEndPoint);
                        return;
                    }
                    if (clients[id].udp.endPoint.ToString() == clientEndPoint.ToString())
                    {
                        clients[id].udp.CheckData(packet);
                    }
                }
            }
            catch (Exception ex)
            {
                Tools.LogError(ex.Message, ex.StackTrace);
            }
        }

        public static void SendDataUDP(IPEndPoint clientEndPoint, Packet packet)
        {
            try
            {
                if (clientEndPoint != null)
                {
                    _udpListener.BeginSend(packet.ToArray(), packet.Length(), clientEndPoint, null, null);
                }
            }
            catch (Exception ex)
            {
                Tools.LogError(ex.Message, ex.StackTrace);
            }
        }

    }
}