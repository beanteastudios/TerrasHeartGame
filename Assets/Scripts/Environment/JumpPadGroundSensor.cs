using UnityEngine;
using TerrasHeart.Environment;

namespace TerrasHeart.Environment
{
    public class JumpPadGroundSensor : MonoBehaviour
    {
        private JumpPad _jumpPad;

        private void Awake()
        {
            _jumpPad = GetComponentInParent<JumpPad>();
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            _jumpPad?.NotifyCollisionEnter(col);
        }

        private void OnCollisionExit2D(Collision2D col)
        {
            _jumpPad?.NotifyCollisionExit(col);
        }
    }
}