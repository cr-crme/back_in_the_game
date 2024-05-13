using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> _scenes;

    public int current { get; private set; } = 0;
    
    // Start is called before the first frame update
    void Start()
    {
        ActivateScene();
    }

    public void ChangeScene(int newScence)
    { 
        current = newScence;
        ActivateScene();
    }

    private void ActivateScene()
    {
        for (int i = 0; i < _scenes.Count; i++)
        {
            _scenes[i].SetActive(i == current);
        }
    }
}
