using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KeyBoard : MonoBehaviour
{
    [SerializeField] TMP_InputField _inputField;
    [SerializeField] List<Button> _keyboardKeys;

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < _keyboardKeys.Count; i++)
        {
            var button = _keyboardKeys[i];
            var key = button.GetComponentInChildren<TMP_Text>().text;
            button.onClick.AddListener(() => OnKeyPressed(key));
        }
    }

    void OnKeyPressed(string key)
    {
        if (key == "<")
        {
            string current = _inputField.text;
            _inputField.text = current.Remove(current.Length - 1, 1); 
        }
        else
        {
            _inputField.text += key;
        }
    }
}
