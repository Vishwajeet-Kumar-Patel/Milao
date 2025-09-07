using System.Collections.Generic;
using UnityEngine;

/// Temporary no-op replacement so the project compiles without Firebase.
/// Remove this class when you wire the real Firebase SDK.
public class CloudFirestore : MonoBehaviour
{
    public static CloudFirestore Instance {
        get {
            if (_instance == null) {
                var go = GameObject.Find("CloudFirestoreStub");
                if (go == null) go = new GameObject("CloudFirestoreStub");
                var comp = go.GetComponent<CloudFirestore>();
                if (comp == null) comp = go.AddComponent<CloudFirestore>();
                _instance = comp;
            }
            return _instance;
        }
        private set { _instance = value; }
    }
    private static CloudFirestore _instance;

    // Very loose shapes to satisfy call sites.
    public Dictionary<int, int> UserLevels { get; private set; } = new Dictionary<int, int>();
    public Dictionary<string, int> UserCollectibles { get; private set; } = new Dictionary<string, int>();

    private void Awake()
    {
    if (_instance == null) _instance = this;
    }

    // --- SAVE / SET methods used around the codebase (no-ops) ---
    public void SaveDataBase(object data) {}
    public void SaveCoinsDataBase(Dictionary<string, object> data) {}
    public void SaveLevelDataBase(object data) {}
    public void SaveLivesDataBase(int lives) {}
    public void SaveLivesDataBase(int lives, params object[] extra) {} // handles 2-arg overloads

    public void SetCollectible(params object[] args) {}
    public void SetUserLevels(params object[] args) {}
    public void UpdateDocumentLevel(params object[] args) {}
    public void UpdateLevelUser(params object[] args) {}

    // --- USER management stubs ---
    public System.Threading.Tasks.Task<bool> CheckUserExists(params object[] args) { return System.Threading.Tasks.Task.FromResult(true); }
    public object GetUserData(params object[] args) { return null; }
    public void CreateNewUser(params object[] args) {}

    // Optional static wrappers if some code calls statically
    public static void SaveDataBase(object data, bool _ = false) { Instance?.SaveDataBase(data); }
    public static void SaveCoinsDataBase(Dictionary<string, object> data, bool _ = false) { Instance?.SaveCoinsDataBase(data); }
    public static void SaveLivesDataBase(int lives, bool _ = false) { Instance?.SaveLivesDataBase(lives); }
    public static void SaveLevelDataBase(object data, bool _ = false) { Instance?.SaveLevelDataBase(data); }
}
