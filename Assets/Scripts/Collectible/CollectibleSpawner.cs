using UnityEngine;
using Hearthwood.Core;

namespace Hearthwood.Collectible
{
    /// <summary>
    /// Spawns a simple cube visual + Collectible component at a given world position.
    /// Used by sample content and editor tools to place collectibles without a prefab.
    /// </summary>
    public sealed class CollectibleSpawner : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField, Tooltip("The collectible definition to attach to spawned instances.")]
        private CollectibleData _data;

        [Header("Visual")]
        [SerializeField, Tooltip("Size of the spawned cube.")]
        [Min(0.01f)]
        private float _cubeSize = 0.3f;

        /// <summary>The collectible data to use for spawned instances.</summary>
        public CollectibleData Data => _data;

        /// <summary>
        /// Creates a GameObject at <paramref name="position"/> with a cube MeshRenderer
        /// and a trigger Collider, then adds a <see cref="Collectible"/> component
        /// configured with <see cref="Data"/>.
        /// </summary>
        /// <param name="position">World position to place the collectible.</param>
        /// <returns>The spawned GameObject, or null if Data is null.</returns>
        public GameObject Spawn(Vector3 position)
        {
            if (_data == null)
            {
                Debug.LogWarning("[CollectibleSpawner] No CollectibleData assigned.");
                return null;
            }

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Collectible_{_data.Id}";
            go.transform.position = position;
            go.transform.localScale = Vector3.one * _cubeSize;

            // Make it a trigger collider.
            var col = go.GetComponent<Collider>();
            col.isTrigger = true;
            col.transform.localScale = Vector3.one * (_cubeSize * 2f); // Larger pickup radius.

            var collectible = go.AddComponent<Collectible>();
            collectible.Configure(_data);

            return go;
        }
    }
}
