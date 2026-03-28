using UnityEngine;

namespace DungeonPrototype.Environment
{
    public class GateController : MonoBehaviour
    {
        [SerializeField] private Transform gateMesh;
        [SerializeField] private Vector3 closedLocalPosition;
        [SerializeField] private Vector3 openLocalPosition = new Vector3(0f, 3f, 0f);
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private Renderer[] slotRenderers;

        private MaterialPropertyBlock _mpb;
        private bool _isOpen;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            if (gateMesh != null)
            {
                gateMesh.localPosition = closedLocalPosition;
            }
        }

        private void Update()
        {
            if (gateMesh == null)
            {
                return;
            }

            Vector3 target = _isOpen ? openLocalPosition : closedLocalPosition;
            gateMesh.localPosition = Vector3.Lerp(gateMesh.localPosition, target, Time.deltaTime * moveSpeed);
        }

        public void SetOpen(bool open, Color essenceColor)
        {
            _isOpen = open;
            UpdateSlotsColor(open ? essenceColor : Color.black);
        }

        private void UpdateSlotsColor(Color color)
        {
            if (slotRenderers == null)
            {
                return;
            }

            for (int i = 0; i < slotRenderers.Length; i++)
            {
                if (slotRenderers[i] == null)
                {
                    continue;
                }

                slotRenderers[i].GetPropertyBlock(_mpb);
                _mpb.SetColor("_EmissionColor", color);
                slotRenderers[i].SetPropertyBlock(_mpb);
            }
        }
    }
}
