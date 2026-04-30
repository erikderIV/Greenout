using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// ============================================================
//  GridUI.cs
//  Rendert einen GridContainer als visuelle Kachelmatrix.
//  Setze dieses Script auf ein leeres GameObject mit einem
//  GridLayoutGroup-Child namens "SlotParent".
//
//  Prefabs die du brauchst:
//    • SlotCellPrefab  – 1×1 Zelle (Image, leicht dunkler Rand)
//    • ItemUIPrefab    – Item-Icon, kann mehrere Zellen belegen
// ============================================================

public class GridUI : MonoBehaviour
{
    [Header("Referenzen")]
    public RectTransform slotParent;       // GridLayoutGroup
    public GameObject    slotCellPrefab;
    public GameObject    itemUIPrefab;
    public RectTransform itemLayer;        // für Items über dem Grid

    [Header("Größe")]
    public float cellSize = 64f;
    public float cellGap  =  2f;

    // Aktuell angebundener Container
    private GridContainer _container;

    // Alle generierten Item-UIs
    private Dictionary<ItemInstance, ItemUI> _itemUIs = new();

    // ── Öffentliche API ─────────────────────────────────────

    public void Bind(GridContainer container)
    {
        _container = container;
        Rebuild();
    }

    public void Refresh()
    {
        // Existierende Item-UIs entfernen
        foreach (var ui in _itemUIs.Values)
            Destroy(ui.gameObject);
        _itemUIs.Clear();

        if (_container == null) return;

        foreach (var item in _container.items)
            SpawnItemUI(item);
    }

    // ── Interne Logik ───────────────────────────────────────

    private void Rebuild()
    {
        // Alle alten Slots löschen
        foreach (Transform child in slotParent)
            Destroy(child.gameObject);

        if (_container == null) return;

        var layout = slotParent.GetComponent<GridLayoutGroup>();
        layout.cellSize    = new Vector2(cellSize, cellSize);
        layout.spacing     = new Vector2(cellGap,  cellGap);
        layout.constraint  = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = _container.width;

        // Slot-Zellen generieren
        for (int y = 0; y < _container.height; y++)
        for (int x = 0; x < _container.width;  x++)
        {
            var cell = Instantiate(slotCellPrefab, slotParent);
            var drop = cell.AddComponent<DropZone>();
            drop.Init(this, x, y);
        }

        Refresh();
    }

    private void SpawnItemUI(ItemInstance item)
    {
        var go = Instantiate(itemUIPrefab, itemLayer);
        var ui = go.GetComponent<ItemUI>();
        ui.Init(item, this);
        _itemUIs[item] = ui;

        PositionItemUI(ui, item);
    }

    public void PositionItemUI(ItemUI ui, ItemInstance item)
    {
        float step = cellSize + cellGap;
        var pos = new Vector2(
            item.gridX * step,
            -item.gridY * step   // UI: y wächst nach unten
        );
        ui.GetComponent<RectTransform>().anchoredPosition = pos;

        // Item-UI-Größe an effektive Grid-Größe anpassen
        var rt = ui.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(
            item.EffectiveWidth  * step - cellGap,
            item.EffectiveHeight * step - cellGap
        );
    }

    // ── Drop-Handling (wird von DropZone aufgerufen) ────────

    public void OnItemDropped(ItemUI itemUI, int targetX, int targetY)
    {
        var item = itemUI.Item;
        var mgr  = InventoryManager.Instance;

        bool placed = mgr.PlaceItem(item, _container, targetX, targetY);

        if (placed)
        {
            // UI aktualisieren
            PositionItemUI(itemUI, item);
            itemUI.transform.SetParent(itemLayer, true);
        }
        else
        {
            // Zurücksnappen
            itemUI.SnapBack();
        }
    }

    // ── Highlighting ────────────────────────────────────────

    public void HighlightCells(int x, int y, int w, int h, bool canPlace)
    {
        // TODO: Zell-Hintergrundfarbe temporär ändern (grün / rot)
        // Iteriere über SlotCell-Komponenten in slotParent
    }

    public void ClearHighlight() { /* Reset cell colors */ }
}
