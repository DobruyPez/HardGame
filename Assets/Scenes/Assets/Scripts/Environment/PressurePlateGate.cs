using UnityEngine;
using DungeonPrototype.Dragon;

namespace DungeonPrototype.Environment
{
    public class PressurePlateGate : MonoBehaviour
    {
        [SerializeField] private GateController gate;
        [SerializeField] private float requiredManaWeight = 60f;
        [SerializeField] private Renderer runeRenderer;

        private MaterialPropertyBlock _mpb;
        private DragonCompanion _dragonOnPlate;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();

            if (gate == null)
            {
                gate = GetComponentInParent<GateController>();
            }

            SetRuneEmission(Color.black);
        }

        private void Update()
        {
            if (gate == null)
            {
                return;
            }

            if (_dragonOnPlate == null)
            {
                gate.SetOpen(false, Color.black);
                return;
            }

            bool worthy = _dragonOnPlate.CurrentMana >= requiredManaWeight;
            Color essence = _dragonOnPlate.EssenceColor;

            gate.SetOpen(worthy, essence);
            SetRuneEmission(worthy ? essence : Color.Lerp(Color.black, essence, 0.25f));
        }

        private void OnTriggerEnter(Collider other)
        {
            DragonCompanion dragon = other.GetComponentInParent<DragonCompanion>();
            if (dragon != null)
            {
                _dragonOnPlate = dragon;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            DragonCompanion dragon = other.GetComponentInParent<DragonCompanion>();
            if (dragon != null && dragon == _dragonOnPlate)
            {
                _dragonOnPlate = null;
                SetRuneEmission(Color.black);
            }
        }

        private void SetRuneEmission(Color color)
        {
            if (runeRenderer == null)
            {
                return;
            }

            runeRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor("_EmissionColor", color);
            runeRenderer.SetPropertyBlock(_mpb);
        }
    }
}
