using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;
using TMPro;

public class HudManager : MonoBehaviour
{
    [SerializeField] 
    protected ControllersActions controllersActions;

    [SerializeField]
    protected TMP_Text textDebug;

    // Update is called once per frame
    void Update()
    {
        if (controllersActions.rightHand != null)
        {
            controllersActions.rightHand.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position);
            textDebug.text = position.ToString();
        } 
        else
        {
            textDebug.text = "No data found";
        }
    }
}
