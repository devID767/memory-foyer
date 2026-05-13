using UnityEngine;
using UnityEditor;

public class EditorUtils
{
    [MenuItem("Tools/Open Persistent Data Path")]
    public static void OpenPersistentDataPath()
    {
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
}
