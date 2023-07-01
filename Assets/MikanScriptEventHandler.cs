using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MikanScriptEventHandler : MonoBehaviour
{
    public Mikan.MikanComponent mikanComponent;
    public AnimationCurve cameraScaleCurve;
    public float cameraScaleTime = 1.0f;

    private Coroutine _currentRoutine;

    // Start is called before the first frame update
    void Start()
    {
        if (mikanComponent != null)
        {
            float startScale = cameraScaleCurve.Evaluate(0);

            mikanComponent.OnMessageEvent += OnMikanMessage;
            mikanComponent.setSceneScale(startScale);
        }
    }

    // Update is called once per frame
    void Update() { }

    public void OnMikanMessage(string message)
    {
        if (message == "resetCameraZoom")
        {
            if (mikanComponent != null)
            {
                float startScale = cameraScaleCurve.Evaluate(0);

                mikanComponent.setSceneScale(startScale);
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

        if (mikanComponent != null)
            mikanComponent.setSceneScale(startScale);

        for (float time = 0; time < cameraScaleTime; time += Time.unscaledDeltaTime)
        {
            float t = time / cameraScaleTime;
            float scaleValue = cameraScaleCurve.Evaluate(t);

            if (mikanComponent != null)
                mikanComponent.setSceneScale(scaleValue);

            yield return null;
        }

        if (mikanComponent != null)
            mikanComponent.setSceneScale(endScale);

        _currentRoutine = null;
    }
}
