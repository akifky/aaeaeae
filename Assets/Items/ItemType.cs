using UnityEngine;

public enum ResourceType { Demir, Komur, Tahta, Tas, Yiyecek }

[CreateAssetMenu(fileName = "Yeni Kaynak", menuName = "Oyun/Kaynak Verisi")]
public class ItemType : ScriptableObject
{
    public string displayName;
    public Sprite icon;
    public Color color = new Color(0,0,0,255);
}