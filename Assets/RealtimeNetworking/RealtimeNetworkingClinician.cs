namespace DevelopersHub.RealtimeNetworking.Client{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using UnityEngine;
    using UnityEngine.UI;
    using DevelopersHub.RealtimeNetworking.Common;
    using TMPro;
        
    public class RealtimeNetworkingClinician : MonoBehaviour
    {
        [SerializeField] SceneManager _sceneManager;
        [SerializeField] TMP_Dropdown _sceneDropdown;
        [SerializeField] List<Transform> _objectsToMove;
        [SerializeField] CsvWriter _objectToSave;

        [SerializeField] Toggle _yFrameToggle;
        [SerializeField] GameObject _yFrame;

        [SerializeField] TMP_InputField _serverIpAddressInput;
        [SerializeField] Button _connectButton;
        [SerializeField] Button _cancelConnectButton;

        [SerializeField] Canvas _connexionPanel;
        [SerializeField] Canvas _controlPanel;
        [SerializeField] Canvas _automaticEnvironmentPanel;
        [SerializeField] Toggle _showAutomaticToggle;

        [SerializeField] ExperimentLoader _experimentLoader;
        [SerializeField] TMP_InputField _savepathInputField;

        [SerializeField] TMP_Text _connexionErrorText;

        bool _isConnected = false;
        bool _isConnecting = false;
        bool _hasRequestedCancelConnexion = false;
        bool _connexionLost = false;
        bool _isProtocolVersionValidated = false;

        // Start is called before the first frame update
        void Start()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            RealtimeNetworking.OnDisconnectedFromServer += OnConnexionLost;
            RealtimeNetworking.OnPacketReceived += PacketReceived;

            _connexionErrorText.gameObject.SetActive(false);

            var previousIp = PlayerPrefs.GetString("IpAddress");
            if (previousIp != null)
            {
                _serverIpAddressInput.text = previousIp;
            }
            ValidateIpAddress();
        }

        void OnApplicationQuit()
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

        public void ToggleYFrameRequest()
        {
            if (!_isConnected)
            {
                return;
            }

            try
            {
                var packet = new Packet();
                packet.Write((int)PacketType.ShowYFrame);
                packet.Write(_yFrameToggle.isOn);
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
                case PacketType.Version:
                    // Change current scene and return the new scene to client
                    var version = packet.ReadString();
                    if (version == null || version != Protocol.version)
                    {
                        _connexionErrorText.gameObject.SetActive(true);
                        _connexionErrorText.text = "Mettre à jour le serveur";
                        _isProtocolVersionValidated = false;
                        RealtimeNetworking.Disconnect();
                    }
                    else
                    {
                        _isProtocolVersionValidated = true;
                    }
                    break;                           

                case PacketType.ChangeScene:
                    // Change current scene and return the new scene to client
                    var newScene = packet.ReadInt();
                    _sceneDropdown.value = newScene;
                    _sceneManager.ChangeScene(newScene);
                    break;


                case PacketType.ShowYFrame:
                    // Change current scene and return the new scene to client
                    var show = packet.ReadBool();
                    _yFrame.SetActive(show);
                    _yFrameToggle.isOn = show;
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

            if (_isConnected && _isProtocolVersionValidated)
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

        public void ToggleAutomaticEnvironment()
        {
            bool value = _showAutomaticToggle.isOn;
            if (value)
            {
                _automaticEnvironmentPanel.gameObject.SetActive(true);
                _sceneDropdown.interactable = false;
                _yFrameToggle.interactable = false;
                _experimentLoader.AddListener(OnExperimentRoundChanged);
            } else
            {
                _experimentLoader.RemoveListener(OnExperimentRoundChanged);
                _yFrameToggle.interactable = true;
                _sceneDropdown.interactable = true;
                _automaticEnvironmentPanel.gameObject.SetActive(false);
            }
        }


        void OnExperimentRoundChanged(int roundIndex, ExperimentRound round, string filename) {
            // The main fallback if anything goes wrong is the waiting room (0)
            _sceneDropdown.value = round != null && (round.scene >= 0 || round.scene < _sceneDropdown.options.Count) ? round.scene : 0;
            ChangeSceneRequest();

            _yFrameToggle.isOn = round != null && round.showYFrame;
            ToggleYFrameRequest();

            _savepathInputField.text = filename == null ? "": filename;
        }
    }
}
