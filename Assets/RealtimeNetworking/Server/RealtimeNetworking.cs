namespace DevelopersHub.RealtimeNetworking.Server
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public class RealtimeNetworking
    {

        public const int port = 5555;
        public const int maxUsers = 1000;
        public const bool udpActive = true;


        #region Events
        public static event ClientCallback OnClientConnected;
        public static event ClientCallback OnClientDisconnected;
        public static event PacketCallback OnPacketReceived;
        #endregion

        #region Callbacks
        public delegate void PacketCallback(int clientId, Packet packet);
        public delegate void ClientCallback(int id, string ip);
        #endregion

        public static void Initialize()
        {
            Application.runInBackground = true;
            Server.Start(maxUsers, port);
        }

        public static void Destroy()
        {
            Server.Stop();
        }

        public static void _ClientConnected(int id, string ip)
        {
            if (OnClientConnected != null)
            {
                OnClientConnected.Invoke(id, ip);
            }
        }

        public static void _ClientDisconnected(int id, string ip)
        {
            if (OnClientDisconnected != null)
            {
                OnClientDisconnected.Invoke(id, ip);
            }
        }


        public static void _PacketReceived(int clientId, Packet packet)
        {
            if (OnPacketReceived != null)
            {
                OnPacketReceived.Invoke(clientId, packet);
            }
        }
    }
}