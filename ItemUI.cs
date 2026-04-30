using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// ============================================================
//  ItemUI.cs
//  Repräsentiert ein Item-Icon im Grid-UI.
//  Unterstützt Drag & Drop und Item-Rotation (R-Taste).
// ============================================================

[RequireComponent(typeof(Image))]
public class ItemUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public ItemInstance Item { get; private set; }

    private GridUI        _parentGrid;
    private RectTransform _rt;
    private Image         _image;
    private Canvas        _rootCanvas;

    private Vector2 _dragStartPos;
    private int     _dragStartX, _dragStartY;

    // Temporärer Container während des Drags (der Root-Canvas)
    private Transform _originalParent;

    // ── Init ─────────────────────────────────────────────────

    public void Init(ItemInstance item, GridUI grid)
    {
        Item        = item;
        _parentGrid = grid;
        _rt         = GetComponent<RectTransform>();
        _image      = GetComponent<Image>();
        _image.sprite = item.definition.icon;
        _rootCanvas = GetComponentInParent<Canvas>().rootCanvas;

        UpdateRotationVisual();
    }

    // ── Drag & Drop ─────────────────────────────────────────

    public void OnBeginDrag(PointerEventData e)
    {
        _dragStartPos = _rt.anchoredPosition;
        _dragStartX   = Item.gridX;
        _dragStartY   = Item.gridY;

        // Item zur Topmost-Canvas-Ebene verschieben (über allem)
        _originalParent = transform.parent;
        transform.SetParent(_rootCanvas.transform, true);

        // Semi-transparent während des Drags
        _image.color = new Color(1, 1, 1, 0.7f);
    }

    public void OnDrag(PointerEventData e)
    {
        // Item der Maus folgen lassen
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rootCanvas.GetComponent<RectTransform>(),
                e.position, e.pressEventCamera, out var localPos))
        {
            _rt.localPosition = localPos;
        }
    }

    public void OnEndDrag(PointerEventData e)
    {
        _image.color = Color.white;

        // Wenn kein Drop-Ziel gefunden → zurücksnappen
        // (DropZone.OnDrop übernimmt, wenn ein Ziel existiert)
        SnapBack();
    }

    /// Springt zur ursprünglichen Position zurück (kein gültiges Ziel).
    public void SnapBack()
    {
        transform.SetParent(_originalParent, true);
        _parentGrid.PositionItemUI(this, Item);
    }

    // ── Rotation ────────────────────────────────────────────

    public void OnPointerClick(PointerEventData e)
    {
        // Rechtsklick = Rotation
        if (e.button == PointerEventData.InputButton.Right)
        {
            Item.isRotated = !Item.isRotated;
            UpdateRotationVisual();
        }
    }

    private void UpdateRotationVisual()
    {
        _rt.localRotation = Item.isRotated
            ? Quaternion.Euler(0, 0, -90f)
            : Quaternion.identity;
    }
}


// ============================================================
//  DropZone.cs
//  Eine einzelne Gitterzelle, die Drop-Events entgegennimmt.
// ============================================================

public class DropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    private GridUI _grid;
    private int    _x, _y;

    public void Init(GridUI grid, int x, int y)
    {
        _grid = grid;
        _x    = x;
        _y    = y;
    }

    public void OnDrop(PointerEventData e)
    {
        var itemUI = e.pointerDrag?.GetComponent<ItemUI>();
        if (itemUI == null) return;

        // Ziel-Grid ermitteln (könnte ein anderer als der Ursprung sein)
        _grid.OnItemDropped(itemUI, _x, _y);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        var itemUI = e.pointerDrag?.GetComponent<ItemUI>();
        if (itemUI == null) return;

        bool canPlace = _grid != null &&
                        InventoryManager.Instance != null;
        // Highlight (vereinfacht – echtes Highlight in GridUI)
        _grid?.HighlightCells(_x, _y,
            itemUI.Item.EffectiveWidth,
            itemUI.Item.EffectiveHeight,
            canPlace);
    }

    public void OnPointerExit(PointerEventData e)
    {
        _grid?.ClearHighlight();
    }
}
