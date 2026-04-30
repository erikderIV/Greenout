using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  InventoryManager.cs  (v2 – Multi-Container)
//
//  Rechts im UI werden jetzt ALLE Container gleichzeitig
//  angezeigt, die aktiv sind:
//    • Equipment-Container (Rucksack, Weste, Rig) – immer sichtbar
//      solange das Item ausgerüstet ist
//    • Externe Container (Kisten, Leichen) – nur wenn der Spieler
//      in Reichweite ist
//
//  Reihenfolge rechts (von oben nach unten):
//    1. Chest-Container  (Weste / Rig)
//    2. Backpack         (Rucksack)
//    3. Pants-Container  (Cargo-Hose mit Taschen)
//    4. Extern           (Kiste, Leiche – wird ganz unten angehängt)
// ============================================================

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    // ── Spieler-Basis-Container ──────────────────────────────
    [Header("Hosentaschen (fix)")]
    public GridContainer pocketLeft = new(4, 2);
    public GridContainer pocketRight = new(4, 2);

    // Equipment-Slots
    private readonly Dictionary<EquipmentSlotType, ItemInstance> _equipSlots = new();

    // ── Aktive Container (rechte Seite) ─────────────────────
    // Geordnete Liste aller Container die rechts angezeigt werden.
    private readonly List<ActiveContainer> _activeContainers = new();

    // Zuletzt geöffneter externer Container (zum Entfernen)
    private ActiveContainer _externalContainer;

    // ── Events ──────────────────────────────────────────────
    public event Action<GridContainer> OnContainerChanged;
    public event Action<EquipmentSlotType, ItemInstance> OnEquipmentChanged;

    /// Wird gefeuert wenn sich die rechte Container-Liste ändert.
    /// Die UI baut daraufhin die rechte Seite komplett neu.
    public event Action<IReadOnlyList<ActiveContainer>> OnActiveContainersChanged;

    // ── Unity-Lifecycle ─────────────────────────────────────

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Grid-Operationen ─────────────────────────────────────

    public bool AddItem(ItemInstance item, GridContainer container)
    {
        if (!container.TryAutoPlace(item)) return false;
        OnContainerChanged?.Invoke(container);
        return true;
    }

    public bool PlaceItem(ItemInstance item, GridContainer container, int x, int y)
    {
        RemoveFromAnyContainer(item);
        if (!container.TryPlace(item, x, y)) return false;
        OnContainerChanged?.Invoke(container);
        return true;
    }

    public bool RemoveItem(ItemInstance item, GridContainer container)
    {
        if (!container.TryRemove(item)) return false;
        OnContainerChanged?.Invoke(container);
        return true;
    }

    // ── Equipment-Slots ─────────────────────────────────────

    public bool Equip(ItemInstance item)
    {
        var slot = item.definition.equipSlot;
        if (slot == EquipmentSlotType.None) return false;

        // Altes Item in diesem Slot zuerst ablegen
        if (_equipSlots.TryGetValue(slot, out var old) && old != null)
            UnequipInternal(slot, old);

        RemoveFromAnyContainer(item);
        _equipSlots[slot] = item;

        // Falls das Item ein Container ist → rechts einblenden
        if (item.definition.isContainer)
            AddEquipmentContainer(slot, item);

        OnEquipmentChanged?.Invoke(slot, item);
        return true;
    }

    public bool Unequip(EquipmentSlotType slot)
    {
        if (!_equipSlots.TryGetValue(slot, out var item) || item == null)
            return false;

        if (!AddItem(item, pocketLeft))
        {
            Debug.LogWarning("Kein Platz zum Ablegen des Items!");
            return false;
        }

        UnequipInternal(slot, item);
        return true;
    }

    public ItemInstance GetEquipped(EquipmentSlotType slot)
        => _equipSlots.TryGetValue(slot, out var i) ? i : null;

    // ── Externer Container (Kiste, Leiche …) ────────────────

    public void OpenExternalContainer(GridContainer container, string label)
    {
        CloseExternalContainer();
        _externalContainer = new ActiveContainer(container, label, isExternal: true);
        _activeContainers.Add(_externalContainer);
        FireActiveContainersChanged();
    }

    public void CloseExternalContainer()
    {
        if (_externalContainer == null) return;
        _activeContainers.Remove(_externalContainer);
        _externalContainer = null;
        FireActiveContainersChanged();
    }

    // ── Hilfsmethoden ────────────────────────────────────────

    /// Priorität bestimmt die Reihenfolge rechts im UI (niedrig = oben).
    private static int ContainerPriority(EquipmentSlotType slot) => slot switch
    {
        EquipmentSlotType.Chest => 0,
        EquipmentSlotType.Backpack => 1,
        EquipmentSlotType.Pants => 2,
        _ => 99
    };

    private void AddEquipmentContainer(EquipmentSlotType slot, ItemInstance item)
    {
        var ac = new ActiveContainer(item.ownContainer,
                                     item.definition.displayName,
                                     isExternal: false,
                                     slot: slot);

        // Sortiert nach Priorität einfügen (externe kommen immer ans Ende)
        int insertAt = _activeContainers.Count;
        for (int i = 0; i < _activeContainers.Count; i++)
        {
            if (_activeContainers[i].IsExternal ||
                ContainerPriority(_activeContainers[i].Slot) > ContainerPriority(slot))
            {
                insertAt = i;
                break;
            }
        }
        _activeContainers.Insert(insertAt, ac);
        FireActiveContainersChanged();
    }

    private void RemoveEquipmentContainer(EquipmentSlotType slot)
    {
        _activeContainers.RemoveAll(c => !c.IsExternal && c.Slot == slot);
        FireActiveContainersChanged();
    }

    private void UnequipInternal(EquipmentSlotType slot, ItemInstance item)
    {
        if (item.definition.isContainer)
            RemoveEquipmentContainer(slot);

        _equipSlots[slot] = null;
        OnEquipmentChanged?.Invoke(slot, null);
    }

    private void RemoveFromAnyContainer(ItemInstance item)
    {
        pocketLeft.TryRemove(item);
        pocketRight.TryRemove(item);
        foreach (var ac in _activeContainers)
            ac.Container.TryRemove(item);
    }

    private void FireActiveContainersChanged()
        => OnActiveContainersChanged?.Invoke(_activeContainers);

    public IReadOnlyList<ActiveContainer> GetActiveContainers()
        => _activeContainers;

    public IEnumerable<GridContainer> AllPlayerContainers()
    {
        yield return pocketLeft;
        yield return pocketRight;
        foreach (var ac in _activeContainers)
            yield return ac.Container;
    }
}

// ============================================================
//  ActiveContainer  – Datenobjekt für einen aktiven Container
// ============================================================

public class ActiveContainer
{
    public GridContainer Container { get; }
    public string Label { get; }
    public bool IsExternal { get; }
    public EquipmentSlotType Slot { get; }

    public ActiveContainer(GridContainer container, string label,
                           bool isExternal, EquipmentSlotType slot = EquipmentSlotType.None)
    {
        Container = container;
        Label = label;
        IsExternal = isExternal;
        Slot = slot;
    }
}