using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

public class UGUITracker: MonoBehaviour
{
    private GTrackSDK _gameTrackSDK;

    private int _curTouchCount = 0;

    private void Awake()
    { 
        _gameTrackSDK = GetComponent<GTrackSDK>();
    }

    private void Start()
    {
        StartCoroutine(ClickTrack());
    }

    private IEnumerator ClickTrack()
    {
        yield return null;
        while (true)
        {
            if (IsPressDown())
            {
                GameObject curPressGameObject = GameTrackMockUpPointerInputModule.GetPointerEventGameObject();
                if (curPressGameObject != null)
                    _gameTrackSDK?.UserClickTrack(GetGameObjectPath(curPressGameObject));
            }
            _curTouchCount = Input.touchCount;
            yield return null;
        }
    }

    private bool IsPressDown()
    {
        if (Input.GetMouseButtonDown(0))
            return true;
        if (Input.touchCount == 1 && _curTouchCount == 0)
            return true;
        return false;
    }

    // optimize code
    private string GetGameObjectPath(GameObject obj)
    {
        if (obj == null) return "null";

        var path = new StringBuilder(obj.name);
        Transform parentTransform = obj.transform.parent;

        while (parentTransform != null)
        {
            path.Insert(0, parentTransform.name + "/");
            parentTransform = parentTransform.parent;
        }

        return path.ToString();
    }
}

public class GameTrackMockUpPointerInputModule : StandaloneInputModule
{
    private static RaycastResult _raycastResult;
    private static List<RaycastResult> _raycastResults = new List<RaycastResult>();

    public static GameObject GetPointerEventGameObject()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        eventData.button = PointerEventData.InputButton.Left;

        _raycastResults.Clear();
        EventSystem.current.RaycastAll(eventData, _raycastResults);
        _raycastResult = BaseInputModule.FindFirstRaycast(_raycastResults);

        return _raycastResult.gameObject;
    }
}