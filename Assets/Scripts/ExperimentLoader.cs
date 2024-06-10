using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;


[System.Serializable]
public class ExperimentRound
{
    public int scene = -1;
    public int task = -1;
    public bool showYFrame = true;
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
        if (experimentName == null || experimentName.Length == 0) return "Experiment name is missing";
        if (sceneNames == null) return "Scene names are missing";
        if (taskNames == null) return "Task names are missing";

        foreach (var round in rounds)
        {
            if (round.scene < 0 || round.scene >= sceneNames.Count) return "Invalid scene index in round";
            if (round.task < 0 || round.task >= taskNames.Count) return "Invalid task index in round";
        }        

        return null;
    }

    public string RoundToString(int index)
    {
        var round = rounds[index];
        return $"{taskNames[round.task]} in {sceneNames[round.scene]}";
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
    [SerializeField] private Button _previousButton;
    [SerializeField] private Text _saveNameText;
    [SerializeField] private Button _nextButton;
    [SerializeField] private Text _errorText;

    public delegate void OnRoundChangedDelegate(int roundIndex, ExperimentRound round, string filename);
    private List<OnRoundChangedDelegate> _onRoundChanged = new List<OnRoundChangedDelegate>();

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
        _saveNameText.text = "No file selected";
        _nextButton.interactable = false;
        _errorText.text = "";
    }
    
    public void GetFileDialog()
    {
        string path = EditorUtility.OpenFilePanel("Select Experiment File", "", "json");
        if (path.Length == 0) return;

        LoadExperiment(path);
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
            _saveNameText.text = "No file selected";
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
            _saveNameText.text = $"Experiment starting {trial}";
            _previousButton.interactable = false;
        }
        if (_currentRound >= _experiment.rounds.Count) {
            _saveNameText.text = $"Experiment complete";
            _nextButton.interactable = false;
        }
        if (_currentRound >= 0 && _currentRound < _experiment.rounds.Count) {
            _saveNameText.text = $"{_experiment.RoundToString(_currentRound)} {trial}";
        }

        NotifyListeners();
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
