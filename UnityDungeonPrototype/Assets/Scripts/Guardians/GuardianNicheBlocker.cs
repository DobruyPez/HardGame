using UnityEngine;

namespace DungeonPrototype.Guardians
{
    public class GuardianNicheBlocker : MonoBehaviour
    {
        [SerializeField] private LayerMask blockerLayers;
        [SerializeField] private Vector3 checkHalfExtents = new Vector3(0.7f, 1f, 0.7f);

        public bool IsBlocked
        {
            get
            {
                Collider[] hits = Physics.OverlapBox(transform.position, checkHalfExtents, transform.rotation, blockerLayers);
                return hits.Length > 0;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, checkHalfExtents * 2f);
            Gizmos.matrix = previous;
        }
    }
}
