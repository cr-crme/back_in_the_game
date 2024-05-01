using System.Collections;
using System.Collections.Generic;
using Unity.XR.PXR;
using UnityEngine;
using UnityEngine.XR;

public class ControllersActions : MonoBehaviour
{

    protected bool m_isSetup = false;
    protected bool m_connectBodyTracker = false;

    public InputDevice head { get; protected set; }
    public InputDevice leftHand { get; protected set; }
    public InputDevice rightHand { get; protected set; }

    public BodyTrackerResult bodyTracker { get; protected set; } = new BodyTrackerResult();

    // Start is called before the first frame update
    void Start()
    {
        SetupXRNodes();
    }

    private void OnApplicationFocus(bool focus)
    {
        if (focus)
        {
            // Set the full-body tracking mode
            PXR_Input.SetSwiftMode(1);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Try to setup again if not setup
        if (!m_isSetup) { 
            SetupXRNodes(); 
            return; 
        }

        // // Get the position of the left hand
        // leftHand.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position);
        // leftHand.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation);
        // Debug.Log(position);
        // Debug.Log(rotation.eulerAngles);
        
        //// Get the triggers button
        //rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool isRightTriggered);
        //if (isRightTriggered)
        //{
        //    Debug.Log("Right trigger button is pressed.");
        //}

        //leftHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool isLeftTriggered);
        //if (isLeftTriggered)
        //{
        //   Debug.Log("Left trigger button is pressed.");
        //}

        // Get the position data of each body joint
        if (m_connectBodyTracker)
        {
            BodyTrackerResult _bodyTracker = new BodyTrackerResult(); 
            PXR_Input.GetBodyTrackingPose(0, ref _bodyTracker);
            bodyTracker = _bodyTracker;
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
        if (!isHeadSet || !isLeftHandSet || !isRightHandSet)
        {
            return;
        }

        if (m_connectBodyTracker)
        {
            // Get the number of motion trackers connected and their IDs.
            PxrFitnessBandConnectState bandConnectState = new PxrFitnessBandConnectState();
            PXR_Input.GetFitnessBandConnectState(ref bandConnectState);

            // If not calibrated, launch the "PICO Motion Tracker" app for calibration
            int calibrated = 0;
            PXR_Input.GetFitnessBandCalibState(ref calibrated);
            if (calibrated == 0)
            {
                PXR_Input.OpenFitnessBandCalibrationAPP();
            }

        }
        m_isSetup = true;
    }
}
