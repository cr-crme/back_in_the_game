
namespace DevelopersHub.RealtimeNetworking.Common
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;
    
    public class CsvWriter : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _subjectNameInput;
        [SerializeField] private TMP_InputField _trialNameInput;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _stopButton;
        [SerializeField] private List<string> _objectsNames;

        public delegate void OnRecordingStartedDelegate(string filepath);
        private List<OnRecordingStartedDelegate> _onRecordingStarted = new List<OnRecordingStartedDelegate>();
        public delegate void OnRecordingStopedDelegate();
        private List<OnRecordingStopedDelegate> _onRecordingStopped = new List<OnRecordingStopedDelegate>();

        public void AddListener(OnRecordingStartedDelegate listener)
        {
            _onRecordingStarted.Add(listener);
        }

        public void RemoveListener(OnRecordingStartedDelegate listener)
        {
            _onRecordingStarted.Remove(listener);
        }
        public void AddListener(OnRecordingStopedDelegate listener)
        {
            _onRecordingStopped.Add(listener);
        }

        public void RemoveListener(OnRecordingStopedDelegate listener)
        {
            _onRecordingStopped.Remove(listener);
        }

        string _filePath { get { return Path.Combine(Application.persistentDataPath, _subjectNameInput.text, $"{_trialNameInput.text}.csv"); } }
        private StreamWriter _fileWriter;
        
        private bool _isRecording = false;
        private List<string> _dataQueue = new List<string>();
        private int _frameCount = 0;
        private const int _framesPerFlush = 100;
        private readonly object _lock = new object(); 

        public class PoseVectors
        {
            public System.Numerics.Vector3 position { get; }
            public System.Numerics.Vector3 rotation { get; }
            
            public PoseVectors(System.Numerics.Vector3 position, System.Numerics.Vector3 rotation)
            {
                this.position = position;
                this.rotation = rotation;
            }

        }

        public class DataEntry
        {
            public float timestamp { get; }
            public List<PoseVectors> poses { get; } = new List<PoseVectors>();

            public DataEntry(float timestamp)
            {
                this.timestamp = timestamp;
            }

            static public string Header(List<string> objectsNames) { 
                var stringOut = "Frame";
                foreach (string name in objectsNames)
                {
                    stringOut += $",{name}_Pos.X,{name}_Pos.Y,{name}_Pos.Z,{name}_Rot.X,{name}_Rot.Y,{name}_Rot.Z";
                }
                return stringOut;
            }

            public override string ToString()
            {
                var stringOut = $"{timestamp:F6}";
                foreach (var item in poses)
                {
                    stringOut += $",{item.position.X:F6},{item.position.Y:F6},{item.position.Z:F6}";
                    stringOut += $",{item.rotation.X:F6},{item.rotation.Y:F6},{item.rotation.Z:F6}";
                }
                return stringOut;
            }
        }

        void Start()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture; // Force the "." to be the decimal separator

            _startButton.interactable = false;
            _startButton.gameObject.SetActive(true);
            _stopButton.interactable = false;
            _stopButton.gameObject.SetActive(false);
        }

        public void ValidateDataPath()
        {
            if (string.IsNullOrEmpty(_subjectNameInput.text) || string.IsNullOrEmpty(_trialNameInput.text))
            {
                _startButton.interactable = false;
                return;
            }

            // Do not allow recording if the file already exists
            if (File.Exists(_filePath))
            {
                _startButton.interactable = false;
                return;
            }

            _startButton.interactable = true;
        }

        public void StartRecording()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath));

            _fileWriter = new StreamWriter(_filePath);
            _fileWriter.WriteLine(DataEntry.Header(_objectsNames));

            _startButton.interactable = false;
            _startButton.gameObject.SetActive(false);
            _stopButton.interactable = true;
            _stopButton.gameObject.SetActive(true);

            _isRecording = true;

            foreach (var listener in _onRecordingStarted)
            {
                listener(_filePath);
            }
        }

        public void StopRecording()
        {
            if (_fileWriter != null)
            {
                FlushDataToFile();
                _fileWriter.Close();
                _fileWriter = null;
            }

            _startButton.interactable = true;
            _startButton.gameObject.SetActive(true);
            _stopButton.interactable = false;
            _stopButton.gameObject.SetActive(false);
            ValidateDataPath();

            _isRecording = false;

            foreach (var listener in _onRecordingStopped)
            {
                listener();
            }
        }

        public void AddData(DataEntry data)
        {
            if (!_isRecording)
            {
                return;
            }
            if (data.poses.Count != _objectsNames.Count)
            {
                Debug.Log("Number of objects to save does not match the provided data.");
            }

            // Add data to the queue
            lock (_lock)
            {
                _dataQueue.Add(data.ToString());

                // Flush data to file every framesPerFlush frames
                _frameCount++;
                if (_frameCount >= _framesPerFlush)
                {
                    FlushDataToFile();
                }
            }
        }

        private void FlushDataToFile()
        {
            lock (_lock)
            {
                foreach (var data in _dataQueue)
                {
                    _fileWriter.WriteLine(data);
                }

                _fileWriter.Flush();
                _dataQueue.Clear();
                _frameCount = 0;
            }

        }

        void OnDestroy()
        {
            StopRecording();
        }
    }
}