namespace DevelopersHub.RealtimeNetworking.Client{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using UnityEngine;
    using UnityEngine.UI;
    using DevelopersHub.RealtimeNetworking.Common;
    using TMPro;
        
    public class RealtimeNetworkingClinician : MonoBehaviour
    {
        [SerializeField] private List<Transform> _objectsToMove;
        [SerializeField] private CsvWriter _objectToSave;

        [SerializeField] private TMP_InputField _serverIpAddressInput;
        [SerializeField] private Button _connectButton;
        [SerializeField] private Button _cancelConnectButton;

        [SerializeField] private Canvas _connexionPanel;
        [SerializeField] private Canvas _controlPanel;

        private bool _isConnecting = false;
        private bool _isConnected = false;

        // Start is called before the first frame update
        void Start()
        {
            RealtimeNetworking.OnDisconnectedFromServer += OnConnexionLost;
            RealtimeNetworking.OnPacketReceived += PacketReceived;

            var previousIp = PlayerPrefs.GetString("IpAddress");
            if (previousIp != null)
            {
                _serverIpAddressInput.text = previousIp;
            }
            ValidateIpAddress();
        }

        private void OnApplicationQuit()
        {
            RealtimeNetworking.Disconnect();
        }

        void FixedUpdate()
        {
            Threading.UpdateMain();
        }

        public void SendInt(int value)
        {
            try 
            {
                var packet = new Packet();
                packet.Write((int)PacketType.Int); 
                packet.Write(value);
                Sender.TCP_Send(packet);
            } catch (System.Exception)
            {
                Debug.Log("Connection lost");
                OnConnexionLost();
            }
        }

        void PacketReceived(Packet packet)
        {
            var packetType = packet.ReadInt();
            switch ((PacketType)packetType)
            {
                case PacketType.CsvWriterDataEntry:
                    var timestamp = packet.ReadFloat();
                    var dataToWrite= new CsvWriter.DataEntry(timestamp);

                    foreach (var item in _objectsToMove)
                    {
                        var position = packet.ReadVector3();
                        var rotation = packet.ReadVector3();
                    
                        item.position = new Vector3(position.X, position.Y, position.Z);
                        item.rotation = Quaternion.Euler(new Vector3(rotation.X, rotation.Y, rotation.Z));

                        dataToWrite.poses.Add(new CsvWriter.PoseVectors(position, rotation));
                    }

                    _objectToSave.AddData(dataToWrite);

                    break;

                default:
                    Debug.Log("Unknown packet type.");
                    break;
            }
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

            string pattern = @"^(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}):(\d{1,5})$";
            Regex regex = new(pattern);
            Match match = regex.Match(_serverIpAddressInput.text);
            if (!match.Success)
            {
                yield break;
            }
            string ip = match.Groups[1].Value;
            int port = int.Parse(match.Groups[2].Value);

            while (!_isConnected && _isConnecting)
            {
                Debug.Log("Trying to Connect...");


                RealtimeNetworking.Connect(ip, port);
                Debug.Log("Failed.");
                // Pause for a second before trying to reconnect
                yield return new WaitForSeconds(5);
            }

            _isConnecting = false;

            RealtimeNetworking.OnConnectingToServerResult -= OnConnectionResult;

            _serverIpAddressInput.interactable = true;
            _connectButton.gameObject.SetActive(true);
            _cancelConnectButton.gameObject.SetActive(false);

            if (_isConnected)
            {
                PlayerPrefs.SetString("IpAddress", _serverIpAddressInput.text);
                _connexionPanel.gameObject.SetActive(false);
                Debug.Log("Connected to server");
            }
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

            string pattern = @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}:\d{1,5}$";
            Regex regex = new(pattern);
            if (!regex.IsMatch(_serverIpAddressInput.text))
            {
                _connectButton.interactable = false;
                return;
            }

            _connectButton.interactable = true;
        }
    }
}