---
paths:
  - "Assets/Scripts/**/*.cs"
---

# Unity Safety

- Do not use Find() / FindObjectOfType() at runtime — use dependency injection
- Cache GetComponent<T>() on own GameObject in Awake(). On other objects (e.g. collision.gameObject) — acceptable in callbacks, but not in Update loops
- Use CompareTag() instead of == "tag"
- Use pre-allocated List<T> overloads for physics queries (Physics.OverlapSphere, Physics.Raycast). NonAlloc methods still work but List<T> overloads are preferred in Unity 6
- Do not use null-coalescing (??) or null-propagation (?.) on Unity objects — Unity overrides == null
- Use == null instead of is null for Unity objects
- Avoid async void — exceptions are silently swallowed. Use async UniTaskVoid or async UniTask with Forget()
