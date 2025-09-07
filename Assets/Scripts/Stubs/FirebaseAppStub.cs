using UnityEngine;

/// Minimal placeholder to satisfy references. Remove when real Firebase is added.
public class FirebaseApp : MonoBehaviour
{
    public static FirebaseApp DefaultInstance { get; private set; }
    // Some code expects `FirebaseApp.Instance`
    public static FirebaseApp Instance => DefaultInstance;

    // Add a stub User property to satisfy references
    public StubUser User { get; set; } = new StubUser();

    public class StubUser {
        public bool IsAnonymous { get; set; } = true;
    }

    public System.Threading.Tasks.Task<bool> LoginAnonymous() { return System.Threading.Tasks.Task.FromResult(true); }
    public void StartFirebaseService(params object[] args) {}
    public void SignOut(params object[] args) {}

    private void Awake()
    {
        if (DefaultInstance == null) DefaultInstance = this;
    }
}
