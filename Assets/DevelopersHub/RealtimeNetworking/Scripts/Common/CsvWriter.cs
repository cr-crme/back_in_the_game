using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DevelopersHub.RealtimeNetworking.Common
{
    public class CsvWriter : MonoBehaviour
    {
        [SerializeField] private TMP_InputField subjectNameInput;
        [SerializeField] private TMP_InputField trialNameInput;
        [SerializeField] private Button startButton;
        [SerializeField] private Button stopButton;

        string FilePath { get { return Path.Combine(Application.persistentDataPath, subjectNameInput.text, $"{trialNameInput.text}.csv"); } }
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
            public float Timestamp { get; }
            public List<PoseVectors> poses { get; } = new List<PoseVectors>();

            public DataEntry(float timestamp)
            {
                Timestamp = timestamp;
            }

            public static string Header { 
                get {
                    return "Frame,Pos.X,Pos.Y,Pos.Z,Rot.X,Rot.Y,Rot.Z";
                }
            }

            public override string ToString()
            {
                var _out = $"{Timestamp:F6}";
                foreach (var item in poses)
                {
                    _out += $",{item.position.X:F6},{item.position.Y:F6},{item.position.Z:F6}";
                    _out += $",{item.rotation.X:F6},{item.rotation.Y:F6},{item.rotation.Z:F6}";
                }
                return _out;
            }
        }

        void Start()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture; // Force the "." to be the decimal separator

            startButton.interactable = false;
            startButton.gameObject.SetActive(true);
            stopButton.interactable = false;
            stopButton.gameObject.SetActive(false);
        }

        public void ValidateDataPath()
        {
            if (string.IsNullOrEmpty(subjectNameInput.text) || string.IsNullOrEmpty(trialNameInput.text))
            {
                startButton.interactable = false;
                return;
            }

            // Do not allow recording if the file already exists
            if (File.Exists(FilePath))
            {
                startButton.interactable = false;
                return;
            }

            startButton.interactable = true;
        }

        public void StartRecording()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));

            _fileWriter = new StreamWriter(FilePath);
            _fileWriter.WriteLine(DataEntry.Header);

            startButton.interactable = false;
            startButton.gameObject.SetActive(false);
            stopButton.interactable = true;
            stopButton.gameObject.SetActive(true);

            _isRecording = true;
        }

        public void StopRecording()
        {
            if (_fileWriter != null)
            {
                FlushDataToFile();
                _fileWriter.Close();
                _fileWriter = null;
            }

            startButton.interactable = true;
            startButton.gameObject.SetActive(true);
            stopButton.interactable = false;
            stopButton.gameObject.SetActive(false);
            ValidateDataPath();

            _isRecording = false;
        }

        public void AddData(DataEntry data)
        {
            if (!_isRecording)
            {
                return;
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