using UnityEngine;

namespace TerrasHeart.Environment
{
    /// <summary>
    /// Handles parallax scrolling and optional wave bob for background layers.
    /// Attach to each parallax child (sky plates, wave layers, etc.)
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [Header("Parallax Settings")]
        [SerializeField, Range(0f, 1f)]
        private float _parallaxSpeed = 0.5f;

        [Header("Auto Scroll")]
        [SerializeField] private bool _autoScroll = false;
        [SerializeField] private float _autoScrollSpeed = 0.3f;

        [Header("Wave Bob")]
        [SerializeField] private bool _enableBob = true;
        [SerializeField] private float _bobAmplitude = 0.05f;
        [SerializeField] private float _bobFrequency = 1f;
        [SerializeField] private float _bobPhaseOffset = 0f;

        private Camera _mainCam;
        private float _startY;
        private float _scrollOffset;
        private float _previousCamX;

        private void Awake()
        {
            _mainCam = Camera.main;
        }

        private void Start()
        {
            _startY = transform.position.y;
            _previousCamX = _mainCam.transform.position.x;
        }

        private void Update()
        {
            // Delta-based parallax — robust at any world position
            float camDeltaX = _mainCam.transform.position.x - _previousCamX;
            _previousCamX = _mainCam.transform.position.x;

            _scrollOffset += camDeltaX * (1f - _parallaxSpeed);

            // Optional auto scroll (waves only — disable for sky layers)
            if (_autoScroll)
                _scrollOffset += _autoScrollSpeed * Time.deltaTime;

            // Vertical bob with per-layer phase offset so layers don't sync
            float bob = 0f;
            if (_enableBob)
                bob = Mathf.Sin((Time.time + _bobPhaseOffset) * _bobFrequency) * _bobAmplitude;

            transform.position = new Vector3(
                _mainCam.transform.position.x - _scrollOffset,
                _startY + bob,
                transform.position.z
            );
        }
    }
}