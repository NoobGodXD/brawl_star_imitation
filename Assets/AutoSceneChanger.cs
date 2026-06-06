using UnityEngine;
using System.Collections;

public class AutoSceneChanger : MonoBehaviour
{
    [Header("設定")]
    public string mainMenuSceneName = "MainMenu"; // 主畫面場景名稱
    public float delayTime = 3f;                  // 出現 Match Over 後等待幾秒

    private SceneMove sceneMove;

    private void Start()
    {
        // 1. 抓取原本就有的 SceneMove 組件
        sceneMove = GetComponent<SceneMove>();
        if (sceneMove == null) sceneMove = gameObject.AddComponent<SceneMove>();

        // 2. 🌟 偷聽 KnockoutGameManager 的比賽結束廣播
        if (KnockoutGameManager.Instance != null)
        {
            KnockoutGameManager.Instance.OnMatchEnd += HandleMatchEnd;
        }
    }

    private void OnDestroy()
    {
        // 養成好習慣，物件毀滅時取消監聽，避免記憶體洩漏
        if (KnockoutGameManager.Instance != null)
        {
            KnockoutGameManager.Instance.OnMatchEnd -= HandleMatchEnd;
        }
    }

    // 當 GameManager 喊出「比賽結束」時，這裡會被觸發
    private void HandleMatchEnd()
    {
        Debug.Log("收到比賽結束廣播，啟動自動返回主畫面倒數...");
        StartCoroutine(DelayAndChangeScene());
    }

    private IEnumerator DelayAndChangeScene()
    {
        // 等待指定秒數，讓玩家看清楚 Match Over 畫面
        yield return new WaitForSeconds(delayTime);

        // 呼叫你原本寫好的 SceneMove 函數切換場景
        sceneMove.ChangeScene(mainMenuSceneName);
    }
}