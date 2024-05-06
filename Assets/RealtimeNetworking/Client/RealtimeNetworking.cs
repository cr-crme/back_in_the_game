namespace DevelopersHub.RealtimeNetworking.Client
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public class RealtimeNetworking
    {

        #region Events
        public static event NoCallback OnDisconnectedFromServer;
        public static event ActionCallback OnConnectingToServerResult;
        public static event PacketCallback OnPacketReceived;
        #endregion

        #region Callbacks
        public delegate void ActionCallback(bool successful);
        public delegate void NoCallback();
        public delegate void PacketCallback(Packet packet);
        #endregion

        private bool _initialized = false;
        private bool _connected = false; public static bool isConnected { get { return instance._connected; } }

        private static RealtimeNetworking _instance = null; public static RealtimeNetworking instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new RealtimeNetworking();
                    _instance.Initialize();
                }
                return _instance;
            }
        }

        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }
            Application.runInBackground = true;
            _initialized = true;
        }

        public static void Connect(string ip, int port)
        {
            Client.instance.ConnectToServer(ip, port);
        }

        public static void Disconnect()
        {
            Client.instance._Disconnect();
        }

        public void _Connection(bool result)
        {
            _connected = result;
            if (OnConnectingToServerResult != null)
            {
                OnConnectingToServerResult.Invoke(result);
            }
        }

        public void _Disconnected()
        {
            _connected = false;
            if (OnDisconnectedFromServer != null)
            {
                OnDisconnectedFromServer.Invoke();
            }
        }

        public void _ReceivePacket(Packet packet)
        {
            if (OnPacketReceived != null)
            {
                OnPacketReceived.Invoke(packet);
            }
        }
    }
}