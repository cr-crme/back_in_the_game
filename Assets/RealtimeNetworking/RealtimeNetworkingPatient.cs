
namespace DevelopersHub.RealtimeNetworking.Server
{
    using System;
    using System.Collections.Generic;
    using DevelopersHub.RealtimeNetworking.Common;
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;

    public class RealtimeNetworkingPatient : MonoBehaviour
    {
        [SerializeField] SceneManager _sceneManager;
        [SerializeField] List<Transform> _objectsToMove;
        [SerializeField] GameObject _yFrame;
        bool _yFrameIsActive = true;

        [SerializeField] Canvas _connexionPanel;
        [SerializeField] TMP_Text _ipAddressText;

        int _nbActiveConnexion = 0;
        bool _connexionStatusChanged = false;
        float _timeStamp = 0.0f; 

        // Start is called before the first frame update
        void Start()
        {
            ShowIpAddress();

            RealtimeNetworking.Initialize();

            RealtimeNetworking.OnClientConnected += ClientConnected;
            RealtimeNetworking.OnClientDisconnected += ClientDisconnected;
            RealtimeNetworking.OnPacketReceived += OnPacketReceived;

            Debug.Log("Server started");

        }

        void OnDestroy()
        {
            RealtimeNetworking.Destroy();
        }

        void FixedUpdate()
        {
            Threading.UpdateMain();

            var packet = new Packet();
            packet.Write((int)PacketType.CsvWriterDataEntry);
            packet.Write(_timeStamp);
            foreach (var item in _objectsToMove)
            {
                packet.Write(
                    new System.Numerics.Vector3(item.localPosition.x, item.localPosition.y, item.localPosition.z)
                );
                packet.Write(
                    new System.Numerics.Vector3(
                        item.localRotation.eulerAngles.x, item.localRotation.eulerAngles.y, item.localRotation.eulerAngles.z)
                );
            }
            Sender.TCP_SentToAll(packet);

            _timeStamp += Time.fixedDeltaTime;
        }

        void Update()
        {
            if (_connexionStatusChanged)
            {
                _connexionPanel.gameObject.SetActive(_nbActiveConnexion == 0);
                _connexionStatusChanged = false;
            }
        }

        void ClientConnected(int id, string ip)
        {
            _connexionStatusChanged = true;
            _nbActiveConnexion++;

            SendCurrentVersion();
            SendCurrentScene();
            SendShowYFrame();


            Debug.Log("Client connected: " + id + " " + ip);
        }

        void ClientDisconnected(int id, string ip)
        {
            _connexionStatusChanged = true;
            _nbActiveConnexion--;
            if (_nbActiveConnexion < 0)
            {
                _nbActiveConnexion = 0;
            }

            Debug.Log("Client disconnected: " + id + " " + ip);
        }

        void SendCurrentVersion()
        {
            var packet = new Packet();
            packet.Write((int)PacketType.Version);
            packet.Write(PacketType.protocolVersion);
            Sender.TCP_SentToAll(packet);
        }

        void SendCurrentScene()
        {
            var packet = new Packet();
            packet.Write((int)PacketType.ChangeScene);
            packet.Write(_sceneManager.current);
            Sender.TCP_SentToAll(packet);
        }

        void SendShowYFrame()
        {
            var packet = new Packet();
            packet.Write((int)PacketType.ShowYFrame);
            packet.Write(_yFrameIsActive);
            Sender.TCP_SentToAll(packet);
        }

        void OnPacketReceived(int id, Packet packet)
        {
            var packetType = packet.ReadInt();
            switch ((PacketType)packetType)
            {
                case PacketType.ChangeScene:
                    // Change current scene and return the new scene to client
                    var newScene = packet.ReadInt();
                    _sceneManager.ChangeScene(newScene);

                    SendCurrentScene();
                    break;

                case PacketType.ShowYFrame:
                    // Change current scene and return the new scene to client
                    _yFrameIsActive = packet.ReadBool();
                    _yFrame.SetActive(_yFrameIsActive);

                    SendShowYFrame();
                    break;

                default:
                    Debug.Log("Unknown packet type.");
                    break;
            }
        }

        void ShowIpAddress()
        {
            List<string> ipAddresses = Tools.FindCurrentIPs();
            if (ipAddresses.Count > 1)
            {
                _ipAddressText.text = $"{ipAddresses[0]}:{RealtimeNetworking.port} (+{ipAddresses.Count - 1})";
            }
            else if (ipAddresses.Count == 0)
            {
                _ipAddressText.text = "Aucune adresse IP trouv�e";
            }
            else
            {
                _ipAddressText.text = $"{ipAddresses[0]}:{RealtimeNetworking.port}";
            }

        }
    }
}