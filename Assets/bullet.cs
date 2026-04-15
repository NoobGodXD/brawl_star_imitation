using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float lifeTime = 1.5f; // 1.5 秒後自動銷毀，避免飛太遠

    void Start()
    {
        // 核心清理機制：物件生成後，開始倒數 lifeTime 秒，時間到就徹底從記憶體中抹除這個 Clone
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter(Collider hitInfo)
    {
        // 如果子彈碰到牆壁或敵人，也應該立刻銷毀自己
        Destroy(gameObject);
    }
}