using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Class used to capture a click on the background. Used to deselect a tangram piece
/// </summary>
public class TangramBGController : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        TangramManager._Instance.DeselectPiece();
    }
}