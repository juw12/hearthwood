namespace Hearthwood.Core
{
    /// <summary>
    /// Implemented by anything that owns a <see cref="PlayerContext"/> snapshot — typically
    /// the active <c>PlayerController3D</c>. Dialogue and quest systems resolve a context
    /// through this interface so they don't reach into concrete player types.
    /// </summary>
    public interface IPlayerContextProvider
    {
        /// <summary>Current snapshot. Null when no player is active in the scene.</summary>
        PlayerContext Current { get; }
    }
}
