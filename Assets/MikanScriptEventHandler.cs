using System.Collections;
using System.IO;
using MikanXR;
using MikanXRPlugin;
using UnityEngine;

public class MikanScriptEventHandler : MonoBehaviour
{
    public MikanManager mikanManager;
    public MikanScene mikanScene;
    public MikanScriptContent scriptContent;
    public float cameraScaleTime = 1.0f;

    private Coroutine _currentRoutine;

    // Start is called before the first frame update
    void Start()
    {
        string appDataPath= Application.dataPath;
        var mikanContentAssetBundle = 
            AssetBundle.LoadFromFile(
                Path.Combine(appDataPath, "__Bundles", "mikancontent"));
        if (mikanContentAssetBundle != null)
        {
            scriptContent = mikanContentAssetBundle.LoadAsset<MikanScriptContent>("BeatSaberMikanContent");
        }
        else
        {
            MikanManager.Instance.Log(MikanLogLevel.Error, "Failed to load AssetBundle!");
        }

        if (mikanManager != null)
        {            
            mikanManager.MikanClient.OnScriptMessage.AddListener(this.OnMikanMessage);
        }

        if (scriptContent != null && mikanScene != null)
        {
            float startScale = scriptContent.cameraZoomInCurve.Evaluate(0);
            mikanScene.CameraPositionScale= startScale;
        }
    }

	private void OnDestroy()
	{
		if (mikanManager != null)
        {
			mikanManager.MikanClient.OnScriptMessage.RemoveListener(this.OnMikanMessage);
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
                float startScale = scriptContent.cameraZoomInCurve.Evaluate(0);

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
        float startScale = scriptContent.cameraZoomInCurve.Evaluate(0);
        float endScale = scriptContent.cameraZoomInCurve.Evaluate(1);

        if (mikanScene != null)
            mikanScene.CameraPositionScale= startScale;

        for (float time = 0; time < cameraScaleTime; time += Time.unscaledDeltaTime)
        {
            float t = time / cameraScaleTime;
            float scaleValue = scriptContent.cameraZoomInCurve.Evaluate(t);

            if (mikanScene != null)
                mikanScene.CameraPositionScale= scaleValue;

            yield return null;
        }

        if (mikanScene != null)
            mikanScene.CameraPositionScale= endScale;

        _currentRoutine = null;
    }
}
