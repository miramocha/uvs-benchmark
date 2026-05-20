using UnityEngine;

namespace Miraluna.Uvs.Benchmarks
{
    public sealed class CounterBehaviour : MonoBehaviour
    {
        public int value;

        private void Update()
        {
            value++;
        }
    }
}
