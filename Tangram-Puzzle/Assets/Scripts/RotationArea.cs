using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Class that generates input based on the rotation area interaction 
/// </summary>
public class RotationArea : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerDownHandler
{
    private float lastPosX; // click position of the last update of the "drag" event
    private float inputRotation = 0; // Rotation area input
    private Rect bounds; // Rotation input region bounds
    private bool onDragEvent = false; // flag indicating that at the beginning of the current frame there was an update of the "drag" event

    public static RotationArea _Instance; // Singleton reference

    void Awake()
    {
        if (_Instance == null)
            _Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        // Getting the rotation input region bounds

        Camera cam = Camera.main;
        RectTransform trans = GetComponent<RectTransform>();
        Vector3[] corners = new Vector3[4];
        trans.GetWorldCorners(corners);
        corners[0] = cam.WorldToScreenPoint(corners[0]);
        corners[2] = cam.WorldToScreenPoint(corners[2]);

        bounds = new Rect(
                corners[0].x,
                corners[0].y,
                corners[2].x - corners[0].x,
                corners[2].y - corners[0].y);
    }

    void LateUpdate()
    {
        // rotation information is only used in the frame in which the drag event occurred, after that, it is reseted
        if (!onDragEvent)
        {
            inputRotation = 0;
        }
        onDragEvent = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        lastPosX = eventData.position.x;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // rotation input is only calculated within the bounds of the rotation input area

        if (bounds.Contains(eventData.position))
        {
            float currentPosX = eventData.position.x;
            inputRotation = currentPosX - lastPosX;
            lastPosX = eventData.position.x;
        }
        else
        {
            inputRotation = 0;
            lastPosX = eventData.position.x;
        }

        onDragEvent = true;
    }

    /// <summary>
    /// Returns the input of the rotation area
    /// </summary>
    /// <returns></returns>
    public float GetInputRotation()
    {
        return inputRotation;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        return;
    }
}