using UnityEngine;

public class LockRotation : MonoBehaviour
{
    // 定義你想要鎖定的世界旋轉角度
    // 對於平躺在地面上的 Sprite，通常是 X: 90, Y: 0, Z: 0
    private Quaternion lockedRotation;

    void Start()
    {
        // 在遊戲開始時紀錄或設定初始旋轉值
        lockedRotation = Quaternion.Euler(0, 0, 0);
    }

    // 使用 LateUpdate 確保在玩家物件轉動之後，我們再強制把角度轉回來
    void LateUpdate()
    {
        // 將物件的世界旋轉強制設定為固定值
        transform.rotation = lockedRotation;
    }
}