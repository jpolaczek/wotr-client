using System;
using UnityEngine;

namespace Wotr.PlayTests
{
    public sealed class EngineScreenPointerPump : MonoBehaviour
    {
        public Action Tick { private get; set; }

        private void Update() => Tick?.Invoke();
    }
}
