# Tarkov-ähnliches Inventarsystem – Setup-Anleitung

## Enthaltene Skripte

| Datei | Zweck |
|---|---|
| `ItemDefinition.cs` | ScriptableObject-Vorlage für jeden Item-Typ |
| `ItemInstance.cs` | Konkrete Item-Instanz (Position, Rotation, UID) |
| `GridContainer.cs` | 2D-Grid-Datenklasse (Platzierungs-Logik) |
| `InventoryManager.cs` | Singleton – verwaltet alle Container & Events |
| `GridUI.cs` | Rendert einen GridContainer als Kacheln |
| `ItemUI.cs` + `DropZone.cs` | Drag & Drop der Items |
| `InventoryUI.cs` + `EquipmentSlotUI.cs` | Haupt-UI-Controller |
| `InteractableContainer.cs` | Welt-Objekt (Kiste, Rucksack) |

---

## Schritt 1 – Prefabs erstellen

### SlotCellPrefab
- Leeres GameObject → `Image` Component
- Größe: 64×64 px
- Hintergrund: leicht dunkles Grau (#2A2A2A)
- Rand: 1 px dunkelgrau
- `DropZone`-Script **nicht** manuell hinzufügen (wird per Code gemacht)

### ItemUIPrefab
- Leeres GameObject → `Image` Component
- Pivot: oben-links (0, 1)  ← wichtig für Grid-Positionierung!
- `ItemUI`-Script hinzufügen

---

## Schritt 2 – InventoryManager in die Szene

1. Leeres GameObject erstellen → nennen: `InventoryManager`
2. Script `InventoryManager.cs` zuweisen
3. Läuft als Singleton, bleibt beim Szenenwechsel erhalten

---

## Schritt 3 – Canvas / UI-Hierarchie aufbauen

```
Canvas (Screen Space – Overlay, Sort Order 10)
└── InventoryWindow
    ├── EquipmentPanel          (links, ca. 200 px breit)
    │   ├── SlotHelmet          Image + EquipmentSlotUI (slotType = Helmet)
    │   ├── SlotChest
    │   ├── SlotPants
    │   └── SlotBoots
    ├── PlayerGridPanel         (Mitte)
    │   ├── LabelPocketLeft     TextMeshPro "Linke Tasche"
    │   ├── GridUI_PocketLeft   GridUI-Script + SlotParent (GridLayoutGroup) + ItemLayer
    │   ├── LabelPocketRight
    │   └── GridUI_PocketRight
    └── ContainerPanel          (rechts, initial deaktiviert)
        ├── ContainerLabel      TextMeshPro
        └── GridUI_Container    GridUI-Script
```

4. `InventoryUI`-Script auf `InventoryWindow` ziehen
5. Alle Referenzen im Inspector verknüpfen

---

## Schritt 4 – Item Definitions anlegen

Rechtsklick im Project-Fenster:
```
Create → Inventory → Item Definition
```

Felder ausfüllen:
- `itemId`: einzigartiger String (z.B. "backpack_alice")
- `gridWidth`/`gridHeight`: wie viele Zellen das Item belegt
- `isContainer`: true für Rucksäcke/Taschen
- `containerWidth`/`containerHeight`: Innengröße des Containers
- `equipSlot`: für Ausrüstungs-Items (Helmet, Chest, …)

---

## Schritt 5 – Interactable Containers in der Welt

1. 3D-Objekt erstellen (z.B. eine Kiste)
2. `InteractableContainer.cs` hinzufügen
3. Im Inspector: Name, Größe, Reichweite einstellen
4. Spieler-Interaktions-System (F-Taste) → `container.Interact()` aufrufen

Beispiel (von deinem PlayerController):
```csharp
void Update()
{
    if (Input.GetKeyDown(KeyCode.F))
    {
        // Raycast vorwärts
        if (Physics.Raycast(Camera.main.transform.position,
                            Camera.main.transform.forward,
                            out RaycastHit hit, 2.5f))
        {
            var container = hit.collider.GetComponent<InteractableContainer>();
            container?.Interact();
        }
    }
}
```

---

## Abhängigkeiten

- **TextMeshPro** (im Package Manager, kostenlos)
- Unity 2022.3 LTS oder neuer empfohlen
- Kein weiteres Package nötig

---

## Nächste Schritte / Erweiterungen

- **Item-Tooltip**: `IPointerEnterHandler` auf `ItemUI` → PopUp mit Stats
- **Item-Stapeln**: `stackSize` in `ItemDefinition` + Stack-Zähler-UI
- **Save/Load**: `JsonUtility.ToJson()` auf alle Container anwenden
- **Gewichts-Anzeige**: `InventoryManager.AllPlayerContainers()` summieren
- **Spieler-Modell**: `OnEquipmentChanged`-Event → 3D-Mesh tauschen
- **Loot-Generierung**: `InteractableContainer` mit zufälligen Items befüllen
