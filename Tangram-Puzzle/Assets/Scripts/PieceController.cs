using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Tangram piece behavior controller class
/// </summary>
public class PieceController : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField]
    private Color mainColor; // main piece color
    [SerializeField]
    private Color dockedColor; // piece color when docked in a solution location

    private readonly float speedRot = 20f; // Tangram piece rotation speed (Deg/s)
    private Vector3 offset = Vector3.zero; // offset between click and piece pivot. It serves to not teleport the pivot to the touch position at the beginning of the "drag"
    private Camera cam; // Main Camera reference
    private float lastTimeClick = -10; // Time between two taps to detect double click
    private SpriteRenderer spRenderer; // Sprite Renderer reference
    private MaterialPropertyBlock propBlock; // MaterialPropertyBlock used to "active" Outline shader propertie
    private bool docked = false; // the piece is docked in a solution location?
    public bool CanFlip { get; set; } // the piece can Flip?
    public int Id { get; set; } // Id used by TangramManager

    void Awake()
    {
        cam = Camera.main;
        spRenderer = GetComponent<SpriteRenderer>();
        propBlock = new MaterialPropertyBlock();
        spRenderer.GetPropertyBlock(propBlock);
        SelectPiece(false);
    }

    void Update()
    {
        DoRotation();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // if clicked, select piece
        Vector3 pos = cam.ScreenToWorldPoint(eventData.position);
        offset = pos - transform.position;
        TangramManager._Instance.SelectPiece(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // canFlip?
        if (!CanFlip)
            return;

        // flipar with double click
        float currentTimeClick = eventData.clickTime;
        if (Mathf.Abs(currentTimeClick - lastTimeClick) < 0.25f)
        {
            spRenderer.flipX = !spRenderer.flipX;
            Collider2D[] colls = GetComponents<Collider2D>();
            colls[0].enabled = !spRenderer.flipX;
            colls[1].enabled = spRenderer.flipX;
            lastTimeClick = -10;
        }
        else
            lastTimeClick = currentTimeClick;

    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Check if piece is docked in a solution location.
        bool removed = TangramManager._Instance.CheckAndRemovePiece(this);
        if (removed)
        {
            spRenderer.color = mainColor;
            docked = false;
        }

        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Update piece position according to click position (with offset)
        Vector3 pos = cam.ScreenToWorldPoint(eventData.position);
        transform.position = pos - offset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // At the end of drag, check if piece can be docked in a solution location
        docked = TangramManager._Instance.CheckAvailableSlot(this, 
            transform.localPosition, 
            transform.localEulerAngles.z, 
            spRenderer.flipX);

        if (docked)
            spRenderer.color = dockedColor;
    }

    /// <summary>
    /// Setup the position/orientation/flip of the piece based on InfoPosition passed as argument
    /// </summary>
    /// <param name="info"></param>
    public void SetInfoPosition(InfoPosition info, Vector2 offset, float rotOffset)
    {
        spRenderer.flipX = info.flip;
        transform.localRotation = Quaternion.Euler(0, 0, info.rotZ + rotOffset);

        Vector3 position = (Vector3)(info.position + offset);
        position.z = -2;
        transform.localPosition = position;
    }

    /// <summary>
    /// Applies rotation (input) if piece is selected
    /// </summary>
    void DoRotation()
    {
        if (TangramManager._Instance.IsPieceSelected(this) && !docked)
        {
            transform.Rotate(Vector3.forward, RotationArea._Instance.GetInputRotation() * speedRot * Time.deltaTime);
        }
    }

    /// <summary>
    /// (De) Select piece (visually)
    /// </summary>
    /// <param name="select"></param>
    public void SelectPiece(bool select)
    {
        propBlock.SetFloat("_Outline", select ? 1f : 0);
        spRenderer.SetPropertyBlock(propBlock);
    }

    /// <summary>
    /// Set order in layer (spriteRenderer)
    /// </summary>
    /// <param name="value"></param>
    public void SetOrderInLayer(int value)
    {
        spRenderer.sortingOrder = value;
    }

    /// <summary>
    /// Get order in layer (spriteRenderer)
    /// </summary>
    /// <returns></returns>
    public int GetOrderInLayer()
    {
        return spRenderer.sortingOrder;
    }

}

/// <summary>
/// Class with Position, Orientation, and flip Information of a Tangram piece
/// </summary>
[Serializable]
public struct InfoPosition
{
    public Vector2 position;
    public float rotZ;
    public bool flip;
}