using UnityEngine;

[CreateAssetMenu(fileName = "GateIconSet", menuName = "CodeBlock/Gate Icon Set")]
public class GateIconSet : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public GridObjectType type;
        public Sprite sprite;
    }

    public Entry[] entries;

    public Sprite GetSprite(GridObjectType type)
    {
        foreach (var e in entries)
            if (e.type == type) return e.sprite;
        return null;
    }
}