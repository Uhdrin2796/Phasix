using System;

/// <summary>
/// Top-level JSON save-file envelope (2026-08 session, see DECISIONS.md -> [Save]). savedAtIso8601
/// is display-only (Save tab UI text, e.g. "Slot 2 — saved Aug 8 2026") — the auto-load-newest-
/// slot logic in SaveSystem.TryGetNewestSlot uses the FILE's own last-write time, not this field,
/// so auto-load still works correctly even if this timestamp were ever missing/stale.
/// </summary>
[Serializable]
public class SaveFile
{
    public PartySaveData party;
    public string savedAtIso8601;
}
