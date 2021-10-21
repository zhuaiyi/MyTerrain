using UnityEngine;

/// <summary>
/// 扩展只考虑了AABB，关于OBB
/// </summary>
public static class GeometryUtilityExtension
{
    /// <summary>
    /// Sphere & AABB intersection Test
    /// </summary>
    /// <param name="geometryUtility"></param>
    /// <param name="sphere"></param>
    /// <param name="aabb"></param>
    /// <returns></returns>
    public static bool TestSphereAABB(Sphere sphere, Bounds aabb)
    {
        float rr = sphere.radius * sphere.radius;
        float ddmin = 0;
        //x轴
        if (sphere.center.x < aabb.min.x)
            ddmin += (sphere.center.x - aabb.min.x) * (sphere.center.x - aabb.min.x);
        else if (sphere.center.x > aabb.max.x)
            ddmin += (sphere.center.x - aabb.max.x) * (sphere.center.x - aabb.max.x);
        //y轴
        if (sphere.center.y < aabb.min.y)
            ddmin += (sphere.center.y - aabb.min.y) * (sphere.center.y - aabb.min.y);
        else if (sphere.center.y > aabb.max.y)
            ddmin += (sphere.center.y - aabb.max.y) * (sphere.center.y - aabb.max.y);
        //z轴
        if (sphere.center.z < aabb.min.z)
            ddmin += (sphere.center.z - aabb.min.z) * (sphere.center.z - aabb.min.z);
        else if (sphere.center.z > aabb.max.z)
            ddmin += (sphere.center.z - aabb.max.z) * (sphere.center.z - aabb.max.z);
        return ddmin <= rr;
    }

    /// <summary>
    /// AABB intersection Test
    /// </summary>
    /// <param name="box1"></param>
    /// <param name="box2"></param>
    /// <returns></returns>
    public static bool TestAABBIntersection(Bounds box1, Bounds box2)
    {
        Vector3 d = box1.center - box2.center;
        float ex = Mathf.Abs(d.x) - (box1.extents.x + box2.extents.x);
        float ey = Mathf.Abs(d.y) - (box1.extents.y + box2.extents.y);
        float ez = Mathf.Abs(d.z) - (box1.extents.z + box2.extents.z);
        return (ex < 0) && (ey < 0) && (ez < 0);
    }
}

public struct Sphere
{
    public Vector3 center;

    public float radius;

    public Sphere(Vector3 center, float radius)
    {
        this.center = center;
        this.radius = radius;
    }
}
