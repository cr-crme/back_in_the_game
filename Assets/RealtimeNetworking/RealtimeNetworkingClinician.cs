namespace DevelopersHub.RealtimeNetworking.Client{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using UnityEngine;
    using UnityEngine.UI;
    using DevelopersHub.RealtimeNetworking.Common;
    using TMPro;
        
    public class RealtimeNetworkingClinician : MonoBehaviour
    {
        [SerializeField] private SceneManager _sceneManager;
        [SerializeField] private TMP_Dropdown _sceneDropdown;
        [SerializeField] private List<Transform> _objectsToMove;
        [SerializeField] private CsvWriter _objectToSave;

        [SerializeField] private TMP_InputField _serverIpAddressInput;
        [SerializeField] private Button _connectButton;
        [SerializeField] private Button _cancelConnectButton;

        [SerializeField] private Canvas _connexionPanel;
        [SerializeField] private Canvas _controlPanel;

        private bool _isConnected = false;
        private bool _isConnecting = false;
        private bool _hasRequestedCancelConnexion = false;
        private bool _connexionLost = false;

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

        void Update()
        {
            if (_connexionLost)
            {
                _connexionPanel.gameObject.SetActive(true);
                _controlPanel.gameObject.SetActive(false);
                _connexionLost=false;
            }
        }

        void FixedUpdate()
        {
            Threading.UpdateMain();
        }

        public void ChangeSceneRequest()
        {
            if (!_isConnected)
            {
                return;
            }

            try
            {
                var packet = new Packet();
                packet.Write((int)PacketType.ChangeScene);
                packet.Write(_sceneDropdown.value);
                Sender.TCP_Send(packet);
            }
            catch (System.Exception)
            {
                Debug.Log("Connection lost");
                OnConnexionLost();
            }
        }

        public void SendInt(int value)
        {
            if (!_isConnected)
            {
                return;
            }

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
                case PacketType.ChangeScene:
                    // Change current scene and return the new scene to client
                    var newScene = packet.ReadInt();
                    _sceneManager.ChangeScene(newScene);
                    break;

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
            _isConnected = false;

            _connexionLost = true;
            Debug.Log("Connexion lost");
        }

        void OnDestroy()
        {
            RealtimeNetworking.Disconnect();
        }

        public void OnQuit()
        {
            Application.Quit();
        }

        public void TryConnecting()
        {
            _cancelConnectButton.gameObject.SetActive(true);
            _serverIpAddressInput.interactable = false;
            _connectButton.gameObject.SetActive(false);

            _hasRequestedCancelConnexion = false;
            _isConnected = false;

            RealtimeNetworking.OnConnectingToServerResult += OnConnectionResult;
            StartCoroutine(TryConnectingCoroutine());
        }

        public void CancelTryConnecting()
        {
            _hasRequestedCancelConnexion = true;

            RealtimeNetworking.OnConnectingToServerResult -= OnConnectionResult;

            _serverIpAddressInput.interactable = true;
            _connectButton.gameObject.SetActive(true);
            _cancelConnectButton.gameObject.SetActive(false);
        }

        System.Collections.IEnumerator TryConnectingCoroutine()
        {
            _isConnecting = true;
            _connectButton.interactable = false;

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

            bool first = true;
            while (!_isConnected && !_hasRequestedCancelConnexion)
            {
                if (!first)
                {
                    Debug.Log("Failed.");
                    // Pause for a second before trying to reconnect
                    yield return new WaitForSeconds(5);
                }

                Debug.Log("Trying to Connect...");
                RealtimeNetworking.Connect(ip, port);
                yield return 0;
                first = false;
            }

            // Call the cancel to reset the layouts even though it is already stopped
            CancelTryConnecting();

            if (_isConnected)
            {
                // If success
                PlayerPrefs.SetString("IpAddress", _serverIpAddressInput.text);
                _connexionPanel.gameObject.SetActive(false);
                _controlPanel.gameObject.SetActive(true);
                Debug.Log("Connected to server");
            }

            _isConnecting = false;
            _hasRequestedCancelConnexion = false;
            _connectButton.interactable = true;
        }

        public void ValidateIpAddress()
        {
            if (_isConnecting)
            {
                _connectButton.interactable = false;
                return;
            }

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
