using MemoryFoyer.Application.Analytics;
using UnityEngine;
using VContainer.Unity;

namespace MemoryFoyer.Composition
{
    internal sealed class SessionTelemetryStartable : IStartable
    {
        private readonly SessionTelemetryListener _listener;

        public SessionTelemetryStartable(SessionTelemetryListener listener)
        {
            _listener = listener;
        }

        public void Start()
        {
            Debug.Log($"[Composition] Telemetry listener active — {_listener.GetType().Name}");
        }
    }
}
