using System.Collections;
using Mikan;
using UnityEngine;

public class MikanScriptEventHandler : MonoBehaviour
{
    public MikanManager mikanManager;
    public MikanScene mikanScene;
    public AnimationCurve cameraScaleCurve;
    public float cameraScaleTime = 1.0f;

    private Coroutine _currentRoutine;

    // Start is called before the first frame update
    void Start()
    {
        if (mikanManager != null)
        {
            float startScale = cameraScaleCurve.Evaluate(0);

            mikanManager.OnMessageEvent += OnMikanMessage;
            mikanScene.CameraPositionScale= startScale;
        }
    }

    // Update is called once per frame
    void Update() { }

    public void OnMikanMessage(string message)
    {
        if (message == "resetCameraZoom")
        {
            if (mikanManager != null)
            {
                float startScale = cameraScaleCurve.Evaluate(0);

                mikanScene.CameraPositionScale= startScale;
            }
        }
        else if (message == "startCameraZoom")
        {
            PlayCameraAnimation();
        }
    }

    public Coroutine PlayCameraAnimation()
    {
        if (_currentRoutine != null)
        {
            StopCoroutine(_currentRoutine);
            _currentRoutine = null;
        }

        _currentRoutine = StartCoroutine(CameraAnimCoroutine());
        return _currentRoutine;
    }

    private IEnumerator CameraAnimCoroutine()
    {
        float startScale = cameraScaleCurve.Evaluate(0);
        float endScale = cameraScaleCurve.Evaluate(1);

        if (mikanScene != null)
            mikanScene.CameraPositionScale= startScale;

        for (float time = 0; time < cameraScaleTime; time += Time.unscaledDeltaTime)
        {
            float t = time / cameraScaleTime;
            float scaleValue = cameraScaleCurve.Evaluate(t);

            if (mikanScene != null)
                mikanScene.CameraPositionScale= scaleValue;

            yield return null;
        }

        if (mikanScene != null)
            mikanScene.CameraPositionScale= endScale;

        _currentRoutine = null;
    }
}
