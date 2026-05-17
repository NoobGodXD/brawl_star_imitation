using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LinearAimIndicator : AimIndicatorBase
{
    [Header("直線瞄準外觀設定")]
    [Tooltip("直線的粗細 (如果變成巨大白色區塊，請調小，例如 0.5)")]
    public float lineWidth = 0.5f;

    private LineRenderer lineRenderer;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.numCapVertices = 5; 
        
        // 確保給予不受光照影響的預設材質
        lineRenderer.material = new Material(Shader.Find("Sprites/Default")); 
    }

    public override void UpdateAiming(Vector3 ownerPosition, Vector3 lookDirection, float range, float angle, Color indicatorColor)
    {
        if (lineRenderer == null) return;

        // 🌟 【修復巨大色塊】一樣加入縮放補償機制
        float scaleFactor = transform.lossyScale.x > 0 ? transform.lossyScale.x : 1f;
        lineRenderer.startWidth = lineWidth / scaleFactor;
        lineRenderer.endWidth = lineWidth / scaleFactor;

        // 起點全亮，終點完全透明
        lineRenderer.startColor = indicatorColor;
        
        Color endFadeColor = indicatorColor;
        endFadeColor.a = 0f; 
        lineRenderer.endColor = endFadeColor;

        Vector3 startPos = ownerPosition + new Vector3(0, 0.1f, 0);
        Vector3 endPos = startPos + (lookDirection.normalized * range);

        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);
    }
}