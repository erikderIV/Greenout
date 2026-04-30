using UnityEngine;

// ============================================================
//  ItemInstance.cs
//  Repräsentiert ein konkretes Item im Inventar.
//  Enthält Position im Grid, Rotation und ggf. eigenen Container.
// ============================================================

[System.Serializable]
public class ItemInstance
{
    public ItemDefinition definition;

    // Position im übergeordneten Grid (obere linke Ecke)
    public int gridX;
    public int gridY;

    // 90°-Drehung (wie in Tarkov: R-Taste)
    public bool isRotated;

    // Falls das Item ein Container ist: sein interner Grid-Inhalt
    public GridContainer ownContainer;

    // UID für Save/Load
    public string instanceId;

    public ItemInstance(ItemDefinition def)
    {
        definition   = def;
        instanceId   = System.Guid.NewGuid().ToString();

        if (def.isContainer)
            ownContainer = new GridContainer(def.containerWidth, def.containerHeight);
    }

    // Effektive Breite / Höhe unter Berücksichtigung der Rotation
    public int EffectiveWidth  => isRotated ? definition.gridHeight : definition.gridWidth;
    public int EffectiveHeight => isRotated ? definition.gridWidth  : definition.gridHeight;
}
