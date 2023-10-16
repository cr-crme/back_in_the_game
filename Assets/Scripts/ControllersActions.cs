using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class ControllersActions : MonoBehaviour
{

    protected bool m_isSetup = false;

    protected InputDevice head;
    protected InputDevice leftHand;
    protected InputDevice rightHand;


    // Start is called before the first frame update
    void Start()
    {
       SetupXRNodes();
    }

    // Update is called once per frame
    void Update()
    {
        // Try to setup again if not setup
        if (!m_isSetup) { 
            SetupXRNodes(); 
            return; 
        }

        // Get the position of the left hand
        leftHand.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position);
        leftHand.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation);
        Debug.Log(position);
        Debug.Log(rotation.eulerAngles);
        
        // Get the triggers button
        rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool isRightTriggered);
        if (isRightTriggered)
        {
            Debug.Log("Right trigger button is pressed.");
        }

        leftHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool isLeftTriggered);
        if (isLeftTriggered)
        {
            Debug.Log("Left trigger button is pressed.");
        }

    }

    protected void SetupXRNodes()
    {
        var inputDevices = new List<UnityEngine.XR.InputDevice>();
        InputDevices.GetDevices(inputDevices);

        bool isHeadSet = false;
        bool isLeftHandSet = false;
        bool isRightHandSet = false;
        foreach (var device in inputDevices)
        {
            if (device.characteristics.HasFlag(InputDeviceCharacteristics.HeadMounted))
            {
                head = device;
                isHeadSet = true;
            }
            else if (
                device.characteristics.HasFlag(InputDeviceCharacteristics.HeldInHand) &&
                device.characteristics.HasFlag(InputDeviceCharacteristics.Controller) &&
                device.characteristics.HasFlag(InputDeviceCharacteristics.Left)
            )
            {
                leftHand = device;
                isLeftHandSet = true;
            }
            else if (
                device.characteristics.HasFlag(InputDeviceCharacteristics.HeldInHand) &&
                device.characteristics.HasFlag(InputDeviceCharacteristics.Controller) &&
                device.characteristics.HasFlag(InputDeviceCharacteristics.Right)
            ){
                rightHand = device;
                isRightHandSet = true;
            }
        }
        if (isHeadSet && isLeftHandSet && isRightHandSet)
        {
            m_isSetup = true;
        }
    }
}
