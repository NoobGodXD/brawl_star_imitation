using UnityEngine;
using UnityEngine.UI;

public class ScoreBoardUI : MonoBehaviour
{
    [Header("比分圓形燈號 (僅 3 個)")]
    public Image[] roundDots;

    [Header("燈號狀態樣式")]
    public Sprite emptyDotSprite;
    public Sprite blueDotSprite;
    public Sprite redDotSprite;

    [Header("局數縮放設定")]
    [Tooltip("當前進行局數的圓點放大倍率")]
    public float enlargedScale = 1.3f;

    // 🌟 修正：接收勝負結果與目前局數，自動更新圖案並放大當前局數燈號
    public void UpdateScoreDots(int[] roundWinners, int activeRound)
    {
        for (int i = 0; i < roundDots.Length; i++)
        {
            if (roundDots[i] == null) continue;

            // 1. 更新圖案
            if (i < roundWinners.Length)
            {
                int winner = roundWinners[i];
                switch (winner)
                {
                    case 0:
                        roundDots[i].sprite = emptyDotSprite;
                        break;
                    case 1:
                        roundDots[i].sprite = blueDotSprite;
                        break;
                    case 2:
                        roundDots[i].sprite = redDotSprite;
                        break;
                }
            }

            // 2. 🌟 縮放控制：代表當前局數的圓形放大，其他圓形縮回 1
            // activeRound 為 1, 2, 3，而陣列索引為 0, 1, 2
            if (i == (activeRound - 1))
            {
                roundDots[i].transform.localScale = Vector3.one * enlargedScale;
            }
            else
            {
                roundDots[i].transform.localScale = Vector3.one;
            }
        }
    }
}