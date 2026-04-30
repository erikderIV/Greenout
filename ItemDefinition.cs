using UnityEngine;

// ============================================================
//  ItemDefinition.cs
//  ScriptableObject – Vorlage für jeden Item-Typ.
//  Rechtsklick im Project → Create → Inventory → Item Definition
// ============================================================

[CreateAssetMenu(menuName = "Inventory/Item Definition", fileName = "New Item")]
public class ItemDefinition : ScriptableObject
{
    [Header("Identität")]
    public string itemId;           // einzigartiger Schlüssel, z.B. "ak74"
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Grid-Größe")]
    [Min(1)] public int gridWidth = 1;   // Spalten, die das Item belegt
    [Min(1)] public int gridHeight = 1;   // Zeilen

    [Header("Eigenschaften")]
    public float weight = 0.1f;
    public bool isContainer;             // Rucksack, Tasche usw.

    [Header("Container-Kapazität (nur wenn isContainer)")]
    [Min(1)] public int containerWidth = 4;
    [Min(1)] public int containerHeight = 4;

    [Header("Ausrüstungs-Slot")]
    public EquipmentSlotType equipSlot = EquipmentSlotType.None;
}

public enum EquipmentSlotType
{
    None,
    Helmet,
    Chest,       // Weste / Rig (hat oft ein eigenes Grid)
    Backpack,    // Rucksack
    Pants,       // Cargo-Hose (kann ein kleines Grid haben)
    Boots
}