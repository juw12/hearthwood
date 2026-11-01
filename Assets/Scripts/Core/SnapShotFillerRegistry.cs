using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hearthwood.Core
{
    /// <summary>
    /// Registry pattern used by <see cref="SaveSystem"/> to gather per-subsystem data
    /// without SaveSystem having to know about Player, Quest, Inventory, etc.
    /// Each subsystem registers a delegate at startup; SaveSystem fans out Capture/Apply.
    /// </summary>
    public static class SnapShotFillerRegistry
    {
        private static readonly List<Action<SaveSystem.Snapshot>> _fillers = new();
        private static readonly List<Action<SaveSystem.Snapshot>> _appliers = new();

        public static void Register(Action<SaveSystem.Snapshot> fill, Action<SaveSystem.Snapshot> apply)
        {
            if (fill != null) _fillers.Add(fill);
            if (apply != null) _appliers.Add(apply);
        }

        public static void Unregister(Action<SaveSystem.Snapshot> fill, Action<SaveSystem.Snapshot> apply)
        {
            if (fill != null) _fillers.Remove(fill);
            if (apply != null) _appliers.Remove(apply);
        }

        internal static void FillInto(SaveSystem.Snapshot snap)
        {
            for (int i = 0; i < _fillers.Count; i++) { try { _fillers[i]?.Invoke(snap); } catch (Exception e) { Debug.LogError($"[SaveSystem] filler {i} threw: {e.Message}"); } }
        }

        internal static void ApplyFrom(SaveSystem.Snapshot snap)
        {
            for (int i = 0; i < _appliers.Count; i++) { try { _appliers[i]?.Invoke(snap); } catch (Exception e) { Debug.LogError($"[SaveSystem] applier {i} threw: {e.Message}"); } }
        }
    }
}
