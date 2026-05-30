using UnityEngine;

namespace SuperHexLink
{
    /// <summary>
    /// Common interface for all spawner components.
    /// Provides a unified way to manage spawning, building, clearing, and refreshing of game elements.
    /// Enables dependency injection and iteration over spawners without tight coupling.
    /// </summary>
    public interface ISpawner
    {
        /// <summary>
        /// Spawns all game elements managed by this spawner.
        /// Typically called once during initial game setup.
        /// </summary>
        void Spawn();

        /// <summary>
        /// Builds game elements from saved state or refreshes existing elements.
        /// Used for save/load operations and runtime updates.
        /// </summary>
        /// <param name="isRefresh">If true, refreshes existing elements; if false, rebuilds from state</param>
        void BuildMe(bool isRefresh);

        /// <summary>
        /// Clears all game elements managed by this spawner.
        /// Removes all spawned objects and resets internal state.
        /// </summary>
        void Clear();

        /// <summary>
        /// Refreshes existing game elements without clearing and rebuilding.
        /// Updates visual state, positions, or properties based on current game state.
        /// </summary>
        void Refresh();
    }
}
