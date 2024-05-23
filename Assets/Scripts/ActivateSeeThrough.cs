using System.Collections;
using System.Collections.Generic;
using Unity.XR.PXR;
using UnityEngine;

public class ActivateSeeThrough : MonoBehaviour
{
    [SerializeField] private Camera _camera;

    void OnEnable()
    {
        _camera.clearFlags = CameraClearFlags.SolidColor;
        PXR_Boundary.EnableSeeThroughManual(true);
    }

    void OnDisable()
    {
        _camera.clearFlags = CameraClearFlags.Skybox;
        PXR_Boundary.EnableSeeThroughManual(false);
    }

}
