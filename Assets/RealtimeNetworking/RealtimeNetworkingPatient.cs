
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
        [SerializeField] private List<Transform> _objectsToMove;

        [SerializeField] private Canvas _connexionPanel;
        [SerializeField] private TMP_Text _ipAddressText;

        private bool _hasNewConnexion = false;
        private bool _hasLostConnexion = false;
        private float _timeStamp = 0.0f; 

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
            if (_hasNewConnexion)
            {
                _connexionPanel.gameObject.SetActive(false);
                _hasNewConnexion = false;
            }    
            if (_hasLostConnexion)
            {
                _connexionPanel.gameObject.SetActive(true);
                _hasLostConnexion = false;
            }
        }

        void ClientConnected(int id, string ip)
        {
            _hasNewConnexion = true;
            Debug.Log("Client connected: " + id + " " + ip);
        }

        void ClientDisconnected(int id, string ip)
        {
            _hasLostConnexion = true;
            _connexionPanel.gameObject.SetActive(true);
            Debug.Log("Client disconnected: " + id + " " + ip);
        }

        void OnPacketReceived(int id, Packet packet)
        {
            Debug.Log("Packet received: " + packet.ReadString());
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
                _ipAddressText.text = "Aucune adresse IP trouvée";
            }
            else
            {
                _ipAddressText.text = $"{ipAddresses[0]}:{RealtimeNetworking.port}";
            }

        }
    }
}