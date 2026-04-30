using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  GridContainer.cs
//  Daten-Klasse für ein 2D-Grid.
//  Funktioniert für: Rucksack, Hosentasche, externe Kisten usw.
// ============================================================

[System.Serializable]
public class GridContainer
{
    public int width;
    public int height;

    // Alle Items, die gerade in diesem Grid liegen
    public List<ItemInstance> items = new();

    // Schnell-Lookup: Welches Item liegt auf Zelle (x,y)?
    private ItemInstance[,] _grid;

    public GridContainer(int w, int h)
    {
        width  = w;
        height = h;
        _grid  = new ItemInstance[w, h];
    }

    // Muss nach Deserialisierung (Load) einmal aufgerufen werden
    public void RebuildGrid()
    {
        _grid = new ItemInstance[width, height];
        foreach (var item in items)
            OccupyCells(item, true);
    }

    // ── Platzierungs-Logik ──────────────────────────────────

    /// Gibt true zurück, wenn das Item an (x,y) passt.
    public bool CanPlace(ItemInstance item, int x, int y)
    {
        int w = item.EffectiveWidth;
        int h = item.EffectiveHeight;

        if (x < 0 || y < 0 || x + w > width || y + h > height)
            return false;

        for (int cx = x; cx < x + w; cx++)
        for (int cy = y; cy < y + h; cy++)
        {
            if (_grid[cx, cy] != null && _grid[cx, cy] != item)
                return false;
        }
        return true;
    }

    /// Legt ein Item an (x,y) ab. Gibt false zurück wenn kein Platz.
    public bool TryPlace(ItemInstance item, int x, int y)
    {
        if (!CanPlace(item, x, y)) return false;

        // Altes Item ggf. aus Grid-Array entfernen (bei Move)
        if (items.Contains(item))
            OccupyCells(item, false);

        item.gridX = x;
        item.gridY = y;
        OccupyCells(item, true);

        if (!items.Contains(item))
            items.Add(item);

        return true;
    }

    /// Entfernt ein Item vollständig aus dem Container.
    public bool TryRemove(ItemInstance item)
    {
        if (!items.Contains(item)) return false;
        OccupyCells(item, false);
        items.Remove(item);
        return true;
    }

    /// Findet automatisch die erste freie Position für das Item.
    public bool TryAutoPlace(ItemInstance item)
    {
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width;  x++)
        {
            if (CanPlace(item, x, y))
                return TryPlace(item, x, y);
        }
        return false;
    }

    /// Item am Slot (x,y) – oder null.
    public ItemInstance GetItemAt(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return null;
        return _grid[x, y];
    }

    // ── Hilfsmethoden ───────────────────────────────────────

    private void OccupyCells(ItemInstance item, bool occupy)
    {
        int w = item.EffectiveWidth;
        int h = item.EffectiveHeight;
        for (int cx = item.gridX; cx < item.gridX + w; cx++)
        for (int cy = item.gridY; cy < item.gridY + h; cy++)
        {
            if (cx >= 0 && cx < width && cy >= 0 && cy < height)
                _grid[cx, cy] = occupy ? item : null;
        }
    }
}
