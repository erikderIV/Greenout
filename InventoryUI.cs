using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// ============================================================
//  InventoryUI.cs  (v2 – Multi-Container)
//
//  Hierarchie im Hierarchy-Panel:
//  InventoryWindow
//  ├── EquipmentPanel           ← links
//  │   ├── SlotHelmet           EquipmentSlotUI
//  │   ├── SlotChest
//  │   ├── SlotBackpack
//  │   ├── SlotPants
//  │   └── SlotBoots
//  ├── PlayerGridPanel          ← Mitte (Hosentaschen)
//  │   ├── GridUI_PocketLeft
//  │   └── GridUI_PocketRight
//  └── ContainerScrollView      ← rechts (ScrollRect)
//      └── Viewport
//          └── ContainerListRoot   ← VerticalLayoutGroup hier
//
//  ContainerEntryPrefab:
//    ├── Header (HorizontalLayoutGroup)
//    │   ├── TitleLabel  (TMP_Text)
//    │   └── CloseButton (Button, nur bei externen Containern)
//    └── GridUIComponent  (GridUI)
// ============================================================

public class InventoryUI : MonoBehaviour
{
    [Header("Equipment-Slots (links)")]
    public EquipmentSlotUI slotHelmet;
    public EquipmentSlotUI slotChest;
    public EquipmentSlotUI slotBackpack;
    public EquipmentSlotUI slotPants;
    public EquipmentSlotUI slotBoots;

    [Header("Spieler-Grids (Mitte)")]
    public GridUI gridPocketLeft;
    public GridUI gridPocketRight;

    [Header("Rechte Seite – Container-Liste")]
    public Transform containerListRoot;   // VerticalLayoutGroup
    public GameObject containerEntryPrefab;

    // Laufende Liste der generierten Entry-GameObjects
    private readonly List<ContainerEntry> _entries = new();

    // ── Unity-Lifecycle ─────────────────────────────────────

    private void Start()
    {
        var mgr = InventoryManager.Instance;

        mgr.OnContainerChanged += OnContainerChanged;
        mgr.OnEquipmentChanged += OnEquipmentChanged;
        mgr.OnActiveContainersChanged += RebuildContainerList;

        gridPocketLeft.Bind(mgr.pocketLeft);
        gridPocketRight.Bind(mgr.pocketRight);

        RefreshEquipmentSlots();

        // Bereits aktive Container anzeigen (z.B. nach Szenen-Reload)
        RebuildContainerList(mgr.GetActiveContainers());
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance == null) return;
        var mgr = InventoryManager.Instance;
        mgr.OnContainerChanged -= OnContainerChanged;
        mgr.OnEquipmentChanged -= OnEquipmentChanged;
        mgr.OnActiveContainersChanged -= RebuildContainerList;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
            gameObject.SetActive(!gameObject.activeSelf);
    }

    // ── Container-Liste rechts ───────────────────────────────

    /// Wird aufgerufen wenn sich _activeContainers im Manager ändert.
    /// Baut die rechte Seite komplett neu (einfach & sicher).
    private void RebuildContainerList(IReadOnlyList<ActiveContainer> containers)
    {
        // Alle alten Einträge löschen
        foreach (var entry in _entries)
            Destroy(entry.gameObject);
        _entries.Clear();

        // Neue Einträge erzeugen
        foreach (var ac in containers)
        {
            var go = Instantiate(containerEntryPrefab, containerListRoot);
            var entry = go.GetComponent<ContainerEntry>();
            entry.Init(ac);
            _entries.Add(entry);
        }
    }

    // ── Event-Handler ────────────────────────────────────────

    private void OnContainerChanged(GridContainer container)
    {
        if (container == InventoryManager.Instance.pocketLeft)
        { gridPocketLeft.Refresh(); return; }

        if (container == InventoryManager.Instance.pocketRight)
        { gridPocketRight.Refresh(); return; }

        // In der Liste suchen und das passende GridUI refreshen
        foreach (var entry in _entries)
        {
            if (entry.BoundContainer == container)
            {
                entry.Grid.Refresh();
                return;
            }
        }
    }

    private void OnEquipmentChanged(EquipmentSlotType slot, ItemInstance item)
    {
        switch (slot)
        {
            case EquipmentSlotType.Helmet: slotHelmet.SetItem(item); break;
            case EquipmentSlotType.Chest: slotChest.SetItem(item); break;
            case EquipmentSlotType.Backpack: slotBackpack.SetItem(item); break;
            case EquipmentSlotType.Pants: slotPants.SetItem(item); break;
            case EquipmentSlotType.Boots: slotBoots.SetItem(item); break;
        }
    }

    // ── Hilfsmethoden ────────────────────────────────────────

    private void RefreshEquipmentSlots()
    {
        var mgr = InventoryManager.Instance;
        slotHelmet.SetItem(mgr.GetEquipped(EquipmentSlotType.Helmet));
        slotChest.SetItem(mgr.GetEquipped(EquipmentSlotType.Chest));
        slotBackpack.SetItem(mgr.GetEquipped(EquipmentSlotType.Backpack));
        slotPants.SetItem(mgr.GetEquipped(EquipmentSlotType.Pants));
        slotBoots.SetItem(mgr.GetEquipped(EquipmentSlotType.Boots));
    }
}


// ============================================================
//  ContainerEntry.cs
//  Steht auf dem containerEntryPrefab.
//  Zeigt Titel + optionalen Schließen-Button + GridUI.
// ============================================================

public class ContainerEntry : MonoBehaviour
{
    [Header("Prefab-Referenzen")]
    public TMP_Text titleLabel;
    public Button closeButton;   // nur bei externen Containern sichtbar
    public GridUI Grid;          // das GridUI-Child

    public GridContainer BoundContainer { get; private set; }

    public void Init(ActiveContainer ac)
    {
        BoundContainer = ac.Container;
        titleLabel.text = ac.Label;

        // Schließen-Button nur bei externen Containern (Kisten, Leichen)
        closeButton.gameObject.SetActive(ac.IsExternal);
        if (ac.IsExternal)
            closeButton.onClick.AddListener(
                () => InventoryManager.Instance.CloseExternalContainer());

        Grid.Bind(ac.Container);
    }
}


// ============================================================
//  EquipmentSlotUI.cs
//  Ein einzelner Ausrüstungs-Slot links im UI.
// ============================================================

public class EquipmentSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [Header("Referenzen")]
    public Image iconImage;
    public EquipmentSlotType slotType;

    private ItemInstance _currentItem;

    public void SetItem(ItemInstance item)
    {
        _currentItem = item;
        iconImage.sprite = item?.definition.icon;
        iconImage.enabled = item != null;
    }

    // Item per Drag & Drop in den Slot ziehen
    public void OnDrop(PointerEventData e)
    {
        var itemUI = e.pointerDrag?.GetComponent<ItemUI>();
        if (itemUI == null) return;

        var item = itemUI.Item;
        if (item.definition.equipSlot != slotType) return;

        InventoryManager.Instance.Equip(item);
    }

    // Doppelklick → ausrüsten ablegen
    public void OnPointerClick(PointerEventData e)
    {
        if (e.clickCount == 2)
            InventoryManager.Instance.Unequip(slotType);
    }
}