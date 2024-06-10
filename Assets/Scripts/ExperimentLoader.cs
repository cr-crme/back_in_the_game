using System;
using System.Collections.Generic;
using System.Globalization;
using SimpleFileBrowser;
using UnityEngine;
using UnityEngine.UI;
using DevelopersHub.RealtimeNetworking.Common;

[System.Serializable]
public class ExperimentRound
{
    public int scene = -1;
    public int task = -1;

    public bool showYFrame = true;

    public bool mustRecord = false;
    public double recordingTime = -1.0;
}

[System.Serializable]
public class Experiment
{
    public string experimentName;
    public List<string> sceneNames;
    public List<string> taskNames;
    public List<ExperimentRound> rounds;

    public string IsValid()
    {
        if (experimentName == null || experimentName.Length == 0) return "Nom de l'expérience manquant";
        if (sceneNames == null) return "Noms des scènes manquants";
        if (taskNames == null) return "Noms des tâches manquants";

        foreach (var round in rounds)
        {
            if (round.scene < 0 || round.scene >= sceneNames.Count) return "Index d'une scène invalide";
            if (round.task < 0 || round.task >= taskNames.Count) return "Index d'une tâche invalide";
        }        

        return null;
    }

    public string RoundToString(int index)
    {
        var round = rounds[index];
        return $"{taskNames[round.task]} dans {sceneNames[round.scene]}";
    }

    public string FileName(int index)
    {
        string ToPascalCase(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
            string[] words = str.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            
            for (int i = 0; i < words.Length; i++)
            {
                // Capitalize the first letter without using ToTitleCase as we want to keep the rest of the word as is
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
                words[i] = System.Text.RegularExpressions.Regex.Replace(words[i], "[^a-zA-Z0-9]", "");
            }


            return string.Join(string.Empty, words);
        }

        // Get the number of experiment value to get a decent padding in the name
        var round = rounds[index];
        int padding = rounds.Count.ToString().Length;
        return $"{(index + 1).ToString().PadLeft(padding, '0')}_{ToPascalCase(taskNames[round.task])}_{ToPascalCase(sceneNames[round.scene])}.csv";
    }
}


public class ExperimentLoader : MonoBehaviour
{
    int _currentRound = -1;
    Experiment _experiment;
    [SerializeField] Button _previousButton;
    [SerializeField] Text _saveNameText;
    [SerializeField] Button _nextButton;
    [SerializeField] Text _errorText;

    [SerializeField] CsvWriter _csvWriter;

    public delegate void OnRoundChangedDelegate(int roundIndex, ExperimentRound round, string filename);
    List<OnRoundChangedDelegate> _onRoundChanged = new List<OnRoundChangedDelegate>();

    public void AddListener(OnRoundChangedDelegate listener)
    {
        _onRoundChanged.Add(listener);
    }

    public void RemoveListener(OnRoundChangedDelegate listener)
    {
        _onRoundChanged.Remove(listener);
    }


    void Start()
    {
        _previousButton.interactable = false;
        _saveNameText.text = "Aucun fichier s�lectionn�";
        _nextButton.interactable = false;
        _errorText.text = "";

        _csvWriter.AddListener(HasRecorded);
    }
    
    public void GetFileDialog()
    {
        FileBrowser.ShowLoadDialog(
            (string[] paths) => LoadExperiment(paths[0]), 
            () => { }, 
            FileBrowser.PickMode.Files, 
            true,
            System.IO.Path.GetDirectoryName(Application.persistentDataPath), 
            "*.json", 
            "Ouvrir exp�rimentation"
        );
    }

    void LoadExperiment(string path)
    {
        _currentRound = -1;
        _experiment = JsonUtility.FromJson<Experiment>(System.IO.File.ReadAllText(path));

        var error = _experiment.IsValid();
        if (error == null) {
            // Happy path
            _errorText.text = "";
            ChangeRound(0);
        } else {
            // Reset to initial state
            _previousButton.interactable = false;
            _saveNameText.text = "Aucun fichier s�lectionn�";
            _nextButton.interactable = false;
            _errorText.text = error;
            _experiment = null;
        }
    }

    public void ChangeRound(int step)
    {
        _previousButton.interactable = true;
        _nextButton.interactable = true;
        _currentRound += step;

        string trial = $"({_currentRound + 1}/{_experiment.rounds.Count})";

        if (_currentRound < 0) {
            _saveNameText.text = $"Début {trial}";
            _previousButton.interactable = false;
        }
        if (_currentRound >= _experiment.rounds.Count) {
            _saveNameText.text = $"Fin";
            _nextButton.interactable = false;
        }
        if (_currentRound >= 0 && _currentRound < _experiment.rounds.Count) {
            _saveNameText.text = $"{_experiment.RoundToString(_currentRound)} {trial}";
            if (_experiment.rounds[_currentRound].mustRecord) _nextButton.interactable = false;
            if (_experiment.rounds[_currentRound].recordingTime > 0) _csvWriter.AddListener(SetupAutomaticStop);
        }

        NotifyListeners();
    }

    void HasRecorded()
    {
        _nextButton.interactable = true;
    }

    void SetupAutomaticStop(string filename)
    {
        if (_experiment.rounds[_currentRound].recordingTime > 0)
        {
            StartCoroutine(AutomaticStopCoroutine(_experiment.rounds[_currentRound].recordingTime));
        }
    }
    System.Collections.IEnumerator AutomaticStopCoroutine(double waitingTime)
    {
        _csvWriter.PreventManualStopping();
        yield return new WaitForSecondsRealtime((float)waitingTime);
        _csvWriter.StopRecording();
    }

        void NotifyListeners()
    {
        ExperimentRound xp;
        string filename;
        if (_currentRound < 0 || _currentRound >= _experiment.rounds.Count) {
            xp = null;
            filename = null;
        } else {
            xp = _experiment.rounds[_currentRound];
            filename = _experiment.FileName(_currentRound);
        }

        foreach (var listener in _onRoundChanged){
            listener(_currentRound, xp, filename);
        }
    }
}
