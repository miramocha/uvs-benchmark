using UnityEngine;

namespace Miraluna.Uvs.Benchmarks
{
    public sealed class RotateBehaviour : MonoBehaviour
    {
        public const float RandomMin = 0f;
        public const float RandomMax = 222f;

        private void Update()
        {
            var angle = Random.Range(RandomMin, RandomMax);
            transform.Rotate(angle, angle, angle, Space.Self);
        }
    }
}
