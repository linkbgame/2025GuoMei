using UnityEngine;
using System.Diagnostics;

public class CameraDebug : MonoBehaviour
{
    private Camera cam;
    private float lastOrtho = float.NaN;
    private float lastFov = float.NaN;

    void Awake()
    {
        cam = Camera.main ?? Camera.current;
        if (cam != null)
        {
            lastOrtho = cam.orthographic ? cam.orthographicSize : float.NaN;
            lastFov = cam.orthographic ? float.NaN : cam.fieldOfView;
        }
    }

    void Update()
    {
        if (cam == null) cam = Camera.main ?? Camera.current;
        if (cam == null) return;

        if (cam.orthographic)
        {
            if (float.IsNaN(lastOrtho)) lastOrtho = cam.orthographicSize;
            if (!Mathf.Approximately(cam.orthographicSize, lastOrtho))
            {
                UnityEngine.Debug.LogFormat("[CameraDebug] orthographicSize changed: {0} -> {1} at frame {2}\nStack:\n{3}",
                    lastOrtho, cam.orthographicSize, Time.frameCount, new StackTrace(true));
                lastOrtho = cam.orthographicSize;
            }
        }
        else
        {
            if (float.IsNaN(lastFov)) lastFov = cam.fieldOfView;
            if (!Mathf.Approximately(cam.fieldOfView, lastFov))
            {
                UnityEngine.Debug.LogFormat("[CameraDebug] fieldOfView changed: {0} -> {1} at frame {2}\nStack:\n{3}",
                    lastFov, cam.fieldOfView, Time.frameCount, new StackTrace(true));
                lastFov = cam.fieldOfView;
            }
        }
    }
}
