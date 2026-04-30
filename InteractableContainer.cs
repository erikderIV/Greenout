using UnityEngine;

// ============================================================
//  InteractableContainer.cs
//  Wird auf Welt-Objekte gelegt (Kisten, Rucksäcke am Boden,
//  NPCs, …). Öffnet das rechte Container-Panel im Inventar.
//
//  Nutzung:
//    1. Objekt in der Szene erstellen (z. B. eine Kiste)
//    2. Dieses Script hinzufügen
//    3. containerName und Größe festlegen
//    4. Spieler interagiert (F-Taste) → Inventar öffnet sich
// ============================================================

public class InteractableContainer : MonoBehaviour
{
    [Header("Container-Einstellungen")]
    public string containerName = "Kiste";
    [Min(1)] public int gridWidth  = 8;
    [Min(1)] public int gridHeight = 6;

    [Header("Interaktions-Reichweite")]
    public float interactRange = 2.5f;

    private GridContainer _container;
    private bool          _isOpen;
    private Transform     _playerTransform;

    private void Awake()
    {
        _container = new GridContainer(gridWidth, gridHeight);

        // Spieler-Referenz holen (Tag "Player" nötig)
        var player = GameObject.FindWithTag("Player");
        if (player) _playerTransform = player.transform;
    }

    private void Update()
    {
        if (!_isOpen) return;

        // Inventar automatisch schließen wenn Spieler zu weit weg
        if (_playerTransform &&
            Vector3.Distance(transform.position, _playerTransform.position) > interactRange)
        {
            Close();
        }
    }

    // F-Taste vom Interaktions-System aufgerufen
    public void Interact()
    {
        if (_isOpen) Close();
        else         Open();
    }

    private void Open()
    {
        _isOpen = true;
        InventoryManager.Instance.OpenExternalContainer(_container, containerName);

        // Inventar-Fenster öffnen falls noch nicht sichtbar
        var invUI = FindFirstObjectByType<InventoryUI>();
        if (invUI && !invUI.gameObject.activeSelf)
            invUI.gameObject.SetActive(true);
    }

    private void Close()
    {
        _isOpen = false;
        InventoryManager.Instance.CloseExternalContainer();
    }

    // Visualisierung der Reichweite im Editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
