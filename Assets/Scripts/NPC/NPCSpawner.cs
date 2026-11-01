using UnityEngine;
using Hearthwood.Core;

namespace Hearthwood.NPC
{
    /// <summary>
    /// Runtime helper that spawns a placeholder NPC (a primitive cube body) wired with an
    /// <see cref="NPCController"/> at a world position, from an <see cref="NPCData"/> asset.
    /// Intended for sample content and quick scene population. Not a singleton.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NPCSpawner : MonoBehaviour
    {
        [Header("Spawn Options")]
        [SerializeField, Tooltip("Scale of the placeholder body cube.")]
        private Vector3 _bodyScale = new Vector3(0.6f, 1.6f, 0.6f);

        [SerializeField, Tooltip("Tint color applied to the body material.")]
        private Color _bodyColor = new Color(0.72f, 0.51f, 0.29f);

        [SerializeField, Tooltip("If true, spawned NPCs are parented to this spawner's transform.")]
        private bool _parentToSpawner = true;

        /// <summary>
        /// Spawn a placeholder NPC (cube body + <see cref="NPCController"/>) at
        /// <paramref name="position"/>.
        /// </summary>
        /// <param name="data">The NPC definition (name, dialogue, wander, faction).</param>
        /// <param name="position">World-space spawn position.</param>
        /// <returns>The created NPC root GameObject, or null if <paramref name="data"/> is null.</returns>
        public GameObject Spawn(NPCData data, Vector3 position)
        {
            if (data == null)
            {
                Debug.LogWarning("[NPCSpawner] Spawn: data is null. Skipping.");
                return null;
            }

            var root = new GameObject("NPC_" + data.Id);
            root.transform.position = position;
            if (_parentToSpawner)
                root.transform.SetParent(transform, true);

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = Vector3.up * (_bodyScale.y * 0.5f);
            body.transform.localScale = _bodyScale;

            var renderer = body.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = _bodyColor;

            var controller = root.AddComponent<NPCController>();
            controller.Bind(data);

            return root;
        }
    }
}
