// ─────────────────────────────────────────────────────────────────────────────
// GlowMantleHUDController.cs
// Path: Assets/Scripts/Creatures/GlowMantleHUDController.cs
// Terra's Heart — In-world UI for the Glow-Mantle call-and-response encounter.
//
// Attach to: a child GameObject of the main Canvas named GlowMantleHUD.
//
// Hierarchy expected:
//   Canvas
//   └── GlowMantleHUD               ← this script + CanvasGroup
//       ├── Swatch_Cyan  (Image)    ← assign to _swatchImages[0]
//       ├── Swatch_Amber (Image)    ← assign to _swatchImages[1]
//       ├── Slot_0       (Image)    ← assign to _slotImages[0]
//       ├── Slot_1       (Image)    ← assign to _slotImages[1]
//       └── StatusText   (TMP)      ← assign to _statusText
//
// GlowMantleAI calls public methods directly.
// Also subscribes to GameEvents.OnPaletteInput to flash swatches on key press.
//
// Phase B Step 4 — graybox functional only. Visual polish is production phase.
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TerrasHeart.Events;

namespace TerrasHeart.Creatures
{
    public class GlowMantleHUDController : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Image components for the two colour swatches — index 0 = Cyan, 1 = Amber.")]
        [SerializeField] private Image[] _swatchImages = new Image[2];

        [Tooltip("Image components for input progress slots — filled by FillSlot().")]
        [SerializeField] private Image[] _slotImages = new Image[2];

        [SerializeField] private TextMeshProUGUI _statusText;

        // ─── Runtime State ────────────────────────────────────────────────────

        private CanvasGroup _canvasGroup;
        private Color[] _paletteColours;
        private Coroutine[] _flashCoroutines;

        private static readonly Color SlotEmpty = new Color(1f, 1f, 1f, 0.15f);
        private static readonly Color SwatchDim = new Color(1f, 1f, 1f, 0.35f);
        private static readonly Color SwatchFull = new Color(1f, 1f, 1f, 1.00f);

        // ─────────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            _flashCoroutines = new Coroutine[_swatchImages.Length];

            _canvasGroup.alpha = 0f;
        }

        private void OnEnable() => GameEvents.OnPaletteInput += HandlePaletteInput;
        private void OnDisable() => GameEvents.OnPaletteInput -= HandlePaletteInput;

        // ─────────────────────────────────────────────────────────────────────
        // Public API — called by GlowMantleAI
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Called by GlowMantleAI during Broadcasting to give the HUD the
        /// colours matching the active GlowMantleConfigSO.
        /// </summary>
        public void Initialise(Color cyanColour, Color amberColour)
        {
            _paletteColours = new Color[] { cyanColour, amberColour };

            if (_swatchImages.Length > 0 && _swatchImages[0] != null)
                _swatchImages[0].color = cyanColour * SwatchDim;
            if (_swatchImages.Length > 1 && _swatchImages[1] != null)
                _swatchImages[1].color = amberColour * SwatchDim;
        }

        public void Show()
        {
            _canvasGroup.alpha = 1f;
            ResetSlots();
            Debug.Log("[GlowMantleHUD] Shown.");
        }

        public void Hide()
        {
            _canvasGroup.alpha = 0f;
        }

        public void SetStatus(string text)
        {
            if (_statusText != null)
                _statusText.text = text;
        }

        /// <summary>Fills the slot at the given step index with the input colour.</summary>
        public void FillSlot(int step, Color colour)
        {
            if (step < 0 || step >= _slotImages.Length) return;
            if (_slotImages[step] != null)
                _slotImages[step].color = colour;
        }

        public void ResetSlots()
        {
            foreach (Image slot in _slotImages)
                if (slot != null)
                    slot.color = SlotEmpty;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Palette Input Handler
        // ─────────────────────────────────────────────────────────────────────

        private void HandlePaletteInput(int index)
        {
            if (_canvasGroup.alpha < 0.5f) return;
            if (index < 0 || index >= _swatchImages.Length) return;

            if (_flashCoroutines[index] != null)
                StopCoroutine(_flashCoroutines[index]);

            _flashCoroutines[index] = StartCoroutine(FlashSwatch(index));
        }

        private IEnumerator FlashSwatch(int index)
        {
            if (_swatchImages[index] == null) yield break;

            Color baseColour = _paletteColours != null && index < _paletteColours.Length
                ? _paletteColours[index]
                : Color.white;

            _swatchImages[index].color = baseColour * SwatchFull;
            yield return new WaitForSeconds(0.12f);
            _swatchImages[index].color = baseColour * SwatchDim;
        }
    }
}