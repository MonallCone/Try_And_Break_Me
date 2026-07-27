using System;
using UnityEngine;

// A tiny helper: fires a callback when this GameObject is destroyed. Lets plain (non-MonoBehaviour)
// classes like ChatController run cleanup (e.g. unregister from ChatRegistry) when their window closes.
public class DestroyRelay : MonoBehaviour
{
    public Action onDestroy;

    private void OnDestroy()
    {
        onDestroy?.Invoke();
    }
}
