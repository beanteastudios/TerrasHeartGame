using System.Collections;
using UnityEngine;
using TerrasHeart.Events;

namespace TerrasHeart.Environment
{
    /// <summary>
    /// Stalactite hanging from cave ceiling. Drops when player enters proximity.
    /// State machine: Idle → Warning (shake) → Falling → Landed (permanent static obstacle).
    ///
    /// SETUP:
    ///   - Rigidbody2D: Body Type = Static initially. Gravity Scale = 0.
    ///   - Collider2D: BoxCollider2D or PolygonCollider2D — NOT trigger.
    ///   - StalactiteConfigSO: assign matching config for standard or instakill variant.
    ///   - Layer: Default.
    ///   - Player tag: DrMaria's GameObject must have the "Player" tag.
    ///
    /// Biome health penalty routes through GameEvents.OnBiomeHealthDelta → BiomeHealthManager.
    /// Never calls BiomeHealthManager directly.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class StalactiteHazard : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────
        // STATE ENUM
        // ─────────────────────────────────────────────────────────────

        private enum State { Idle, Warning, Falling, Landed }

        // ─────────────────────────────────────────────────────────────
        // SERIALIZED FIELDS
        // ─────────────────────────────────────────────────────────────

        [Header("Config")]
        [SerializeField] private StalactiteConfigSO _config;

        [Header("Detection")]
        [Tooltip("Player physics layer mask. Set to the layer DrMaria occupies.")]
        [SerializeField] private LayerMask _playerLayer;

        // ─────────────────────────────────────────────────────────────
        // PRIVATE REFERENCES
        // ─────────────────────────────────────────────────────────────

        private Rigidbody2D _rb;
        private State _currentState = State.Idle;
        private bool _proximityTriggered;

        // ─────────────────────────────────────────────────────────────
        // LIFECYCLE
        // ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Static;
            _rb.gravityScale = 0f;
        }

        // ─────────────────────────────────────────────────────────────
        // UPDATE — proximity check while idle
        // ─────────────────────────────────────────────────────────────

        private void Update()
        {
            if (_currentState != State.Idle || _proximityTriggered) return;
            if (_config == null) return;

            Collider2D hit = Physics2D.OverlapCircle(
                transform.position, _config.ProximityRadius, _playerLayer);

            if (hit != null)
            {
                _proximityTriggered = true;
                StartCoroutine(WarningRoutine());
            }
        }

        // ─────────────────────────────────────────────────────────────
        // FIXED UPDATE — drive fall velocity
        // ─────────────────────────────────────────────────────────────

        private void FixedUpdate()
        {
            if (_currentState == State.Falling)
                _rb.linearVelocity = Vector2.down * _config.FallSpeed;
        }

        // ─────────────────────────────────────────────────────────────
        // WARNING ROUTINE
        // ─────────────────────────────────────────────────────────────

        private IEnumerator WarningRoutine()
        {
            _currentState = State.Warning;

            Vector3 origin = transform.position;
            float elapsed = 0f;

            const float ShakeMagnitude = 0.0625f;
            const float ShakeFrequency = 30f;

            while (elapsed < _config.WarningDuration)
            {
                elapsed += Time.deltaTime;
                float offsetX = Mathf.Sin(elapsed * ShakeFrequency) * ShakeMagnitude;
                transform.position = origin + new Vector3(offsetX, 0f, 0f);
                yield return null;
            }

            transform.position = origin;
            TransitionToFalling();
        }

        // ─────────────────────────────────────────────────────────────
        // STATE TRANSITIONS
        // ─────────────────────────────────────────────────────────────

        private void TransitionToFalling()
        {
            _currentState = State.Falling;
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 0f;
            _rb.linearVelocity = Vector2.down * _config.FallSpeed;

            // Ignore all colliders currently overlapping or touching this stalactite
            // This prevents it from immediately landing on the ceiling it's embedded in
            Collider2D myCollider = GetComponent<Collider2D>();
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(LayerMask.GetMask("Ground", "Default"));
            filter.useLayerMask = true;

            Collider2D[] overlapping = new Collider2D[10];
            int count = Physics2D.OverlapCollider(myCollider, filter, overlapping);
            for (int i = 0; i < count; i++)
                Physics2D.IgnoreCollision(myCollider, overlapping[i], true);

            GameEvents.RaiseStalactiteFall(transform.position);
        }

        private void TransitionToLanded()
        {
            _currentState = State.Landed;
            _rb.linearVelocity = Vector2.zero;
            _rb.bodyType = RigidbodyType2D.Static;

            // Ecological consequence — route through event bus
            GameEvents.RaiseStalactiteLanded(transform.position);
            GameEvents.RaiseBiomeHealthDelta(_config.BiomeID, -_config.BiomeHealthPenalty);
        }

        // ─────────────────────────────────────────────────────────────
        // COLLISION — hit player or land on ground
        // ─────────────────────────────────────────────────────────────

        private void OnCollisionEnter2D(Collision2D col)
        {
            if (_currentState != State.Falling) return;

            if (col.gameObject.CompareTag("Player"))
            {
                int finalDamage = _config.IsInstakill ? 80 : _config.Damage;
                // TODO: route to HealthManager when it exists
                Debug.Log($"[StalactiteHazard] Player hit for {finalDamage} damage. (HealthManager stub)");
                // Stalactite continues falling after hitting player
                return;
            }

            // Any other solid contact = ground — land and become permanent obstacle
            TransitionToLanded();
        }

        // ─────────────────────────────────────────────────────────────
        // EDITOR GIZMOS
        // ─────────────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_config == null) return;
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, _config.ProximityRadius);
        }
#endif
    }
}