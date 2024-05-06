
namespace DevelopersHub.RealtimeNetworking.Client
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using DevelopersHub.RealtimeNetworking.Common;
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;

    public class RealtimeNetworkingClient : MonoBehaviour
    {        
        [SerializeField] private List<Transform> _objectsToMove;

        [SerializeField] private Canvas _connexionPanel;
        [SerializeField] private TMP_InputField _serverIpAddressInput;
        [SerializeField] private int _serverPort = 5555;
        [SerializeField] private Button _connectButton;
        [SerializeField] private Button _cancelConnectButton;

        private bool _isConnecting = false;
        private bool _isConnected = false;
        private float _timeStamp = 0.0f; 

        // Start is called before the first frame update
        void Start()
        {
            RealtimeNetworking.OnDisconnectedFromServer += OnConnexionLost;
            RealtimeNetworking.OnPacketReceived += OnPacketReceived;

            ValidateIpAddress();
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        void FixedUpdate()
        {
            Threading.UpdateMain();

            if (!_isConnected) return;

            try
            {
                var packet = new Packet();
                packet.Write((int)PacketType.CsvWriterDataEntry);
                packet.Write(_timeStamp);
                foreach (var item in _objectsToMove)
                {
                    packet.Write(item.localPosition);
                    packet.Write(item.localRotation.eulerAngles);
                }
                Sender.TCP_Send(packet);

            } catch (System.Exception)
            {
                Debug.Log("Connection lost");
                OnConnexionLost();
            }

            _timeStamp += Time.fixedDeltaTime;
        }

        public void ValidateIpAddress()
        {
            if (_serverIpAddressInput == null)
            {
                _connectButton.interactable = false;
                return;
            }

            _serverIpAddressInput.text = _serverIpAddressInput.text.Trim();
            if (string.IsNullOrEmpty(_serverIpAddressInput.text))
            {
                _connectButton.interactable = false;
                return;
            }

            string pattern = @"^(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";
            Regex regex = new Regex(pattern);
            if (!regex.IsMatch(_serverIpAddressInput.text))
            {
                _connectButton.interactable = false;
                return;
            }

            _connectButton.interactable = true;
        }

        void OnConnectionResult(bool success)
        {
            _isConnected = success;
        }

        void OnConnexionLost()
        {
            _isConnecting = false;
            _isConnected = false;

            _connexionPanel.gameObject.SetActive(true);
        }

        public void TryConnecting()
        {
            _serverIpAddressInput.interactable = false;
            _connectButton.gameObject.SetActive(false);
            _cancelConnectButton.gameObject.SetActive(true);

            _isConnecting = true;
            _isConnected = false;

            _timeStamp = 0.0f;
            RealtimeNetworking.OnConnectingToServerResult += OnConnectionResult;

            StartCoroutine(TryConnectingCoroutine());
        }

        public void CancelTryConnecting()
        {
            _isConnecting = false;
        }

        System.Collections.IEnumerator TryConnectingCoroutine()
        {
            // Just make sure it is disconnected
            RealtimeNetworking.Disconnect();

            while (!_isConnected && _isConnecting)
            {
                Debug.Log("Trying to Connect...");
                RealtimeNetworking.Connect(_serverIpAddressInput.text, _serverPort);
                // Pause for a second before trying to reconnect
                yield return new WaitForSeconds(1);
            }

            _isConnecting = false;

            RealtimeNetworking.OnConnectingToServerResult -= OnConnectionResult;

            _serverIpAddressInput.interactable = true;
            _connectButton.gameObject.SetActive(true);
            _cancelConnectButton.gameObject.SetActive(false);
            
            if (_isConnected)
            {
                Debug.Log("Connected to server");
                _connexionPanel.gameObject.SetActive(false);
            }
        }

        void OnPacketReceived(Packet packet)
        {
            Debug.Log("Packet received: " + packet.ReadString());
        }

        
        private void OnApplicationQuit()
        {
            RealtimeNetworking.Disconnect();
        }
    }
}