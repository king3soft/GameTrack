using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

public class UGUITracker: MonoBehaviour
{
    private GameTrackSDK _gameTrackSDK;

    private int _curTouchCount = 0;
    
    private void Awake()
    {
        _gameTrackSDK = GetComponent<GameTrackSDK>();
    }

    private void Start()
    {
        StartCoroutine(ClickTrack());
    }

    private IEnumerator ClickTrack()
    {
        while (true)
        {
            if (IsPressDown())
            {
                Vector2 pos = Input.mousePosition;
                Touch touch = new Touch { position = pos };
                PointerEventData pointerEventData = MockUpPointerInputModule.GetPointerEventData(touch);
                if (pointerEventData.pointerPress != null)
                {
                    GameObject curPressGameObject = pointerEventData.pointerPress;
                    _gameTrackSDK?.UserClickTrack(GetGameObjectPath(curPressGameObject));
                }
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
    /*
    private string GetGameObjectPath(GameObject obj)
    {
        if (obj == null) return "null";
        string path = "/" + obj.name;
        Transform parentTransform = obj.transform.parent;
        while (parentTransform != null)
        {
            path = "/" + parentTransform.name + path;
            parentTransform = parentTransform.parent;
        }
        return path;
    }
    */
    
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