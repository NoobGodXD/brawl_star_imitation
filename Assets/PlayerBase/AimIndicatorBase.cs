using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public abstract class AimIndicatorBase : MonoBehaviour
{
    public abstract void UpdateAiming(Vector3 ownerPosition, Vector3 lookDirection, float range, float angle, Color indicatorColor);
}