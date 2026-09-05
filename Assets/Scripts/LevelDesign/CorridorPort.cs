using UnityEngine;

namespace Echo.LevelDesign
{
    [DisallowMultipleComponent]
    public sealed class CorridorPort : MonoBehaviour
    {
        [SerializeField] private float width;
        [SerializeField] private float height;
        public float Width => width;
        public float Height => height;
#if UNITY_EDITOR
        public void SetDimensions(float openingWidth, float openingHeight)
        {
            width = openingWidth;
            height = openingHeight;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            var previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.up * height * 0.5f, new Vector3(width, height, 0f));
            Gizmos.DrawRay(Vector3.up * 0.1f, Vector3.forward);
            Gizmos.matrix = previous;
        }
#endif
    }
}
