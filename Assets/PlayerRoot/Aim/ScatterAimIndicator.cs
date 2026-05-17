using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(LineRenderer))]
public class ScatterAimIndicator : AimIndicatorBase
{
    [Header("幾何與精度設定")]
    [Range(10, 50)] public int angularSegments = 30;
    [Range(2, 20)] public int radialSegments = 10;
    
    [Tooltip("漸層厚度：決定漸層往內延伸多深")]
    public float gradientThickness = 0.25f;

    private Mesh mesh;
    private LineRenderer lineRenderer;

    void Awake()
    {
        mesh = new Mesh();
        mesh.name = "ScatterAimMesh";
        GetComponent<MeshFilter>().mesh = mesh;
        
        // 🌟 【防呆機制】自動幫網格穿上支援「頂點漸層」的材質，徹底消滅洋紅色！
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer.sharedMaterial == null || meshRenderer.sharedMaterial.name == "Default-Material")
        {
            meshRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }

        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        
        // 🌟 【防呆機制】自動幫外框線穿上材質，徹底消滅洋紅色！
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
    }

    public override void UpdateAiming(Vector3 ownerPosition, Vector3 lookDirection, float range, float angle, Color indicatorColor)
    {
        DrawAimArea(range, angle, indicatorColor);
    }

    private void DrawAimArea(float currentRadius, float angle, Color baseColor)
    {
        // 完美還原原版的顏色漸層邏輯
        Color edgeColor = baseColor;            
        Color centerColor = baseColor;          
        centerColor.a = 0f;                     
        Color borderColor = baseColor;
        borderColor.a = 1f;

        float halfAngle = angle / 2f;

        int numVertices = (radialSegments + 1) * (angularSegments + 1);
        Vector3[] vertices = new Vector3[numVertices];
        Color[] colors = new Color[numVertices];
        int[] triangles = new int[radialSegments * angularSegments * 6];

        int vIndex = 0;
        int tIndex = 0;

        for (int r = 0; r <= radialSegments; r++)
        {
            float radiusRatio = (r / (float)radialSegments) * currentRadius;

            for (int a = 0; a <= angularSegments; a++)
            {
                float currentAngle = -halfAngle + (a / (float)angularSegments) * angle;
                float angleRad = currentAngle * Mathf.Deg2Rad;

                vertices[vIndex] = new Vector3(Mathf.Sin(angleRad) * radiusRatio, 0.01f, Mathf.Cos(angleRad) * radiusRatio);

                // 完美還原原版的數學漸層計算
                float distArc = currentRadius - radiusRatio;
                float angleToLeft = currentAngle - (-halfAngle);
                float angleToRight = halfAngle - currentAngle;
                float distLeft = radiusRatio * Mathf.Sin(angleToLeft * Mathf.Deg2Rad);
                float distRight = radiusRatio * Mathf.Sin(angleToRight * Mathf.Deg2Rad);

                float minDist = Mathf.Min(distArc, Mathf.Min(distLeft, distRight));
                float fadeRatio = Mathf.Clamp01(minDist / gradientThickness);
                
                colors[vIndex] = Color.Lerp(edgeColor, centerColor, fadeRatio);

                if (r < radialSegments && a < angularSegments)
                {
                    int current = vIndex;
                    int next = current + 1;
                    int above = current + (angularSegments + 1);
                    int aboveNext = above + 1;

                    triangles[tIndex++] = current;
                    triangles[tIndex++] = above;
                    triangles[tIndex++] = next;

                    triangles[tIndex++] = next;
                    triangles[tIndex++] = above;
                    triangles[tIndex++] = aboveNext;
                }
                vIndex++;
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        // 完美還原原版的 LineRenderer 設置
        Vector3[] linePoints = new Vector3[angularSegments + 3];
        linePoints[0] = new Vector3(0, 0.02f, 0); 
        for (int a = 0; a <= angularSegments; a++)
        {
            float currentAngle = -halfAngle + (a / (float)angularSegments) * angle;
            float angleRad = currentAngle * Mathf.Deg2Rad;
            linePoints[a + 1] = new Vector3(Mathf.Sin(angleRad) * currentRadius, 0.02f, Mathf.Cos(angleRad) * currentRadius);
        }
        linePoints[angularSegments + 2] = new Vector3(0, 0.02f, 0);

        lineRenderer.positionCount = linePoints.Length;
        lineRenderer.SetPositions(linePoints);
        
        // 🌟 【精準鎖死】直接鎖定 0.08f，絕對不會再變成巨大色塊！
        lineRenderer.startWidth = 0.08f;
        lineRenderer.endWidth = 0.08f;
        lineRenderer.startColor = borderColor;
        lineRenderer.endColor = borderColor;
    }
}