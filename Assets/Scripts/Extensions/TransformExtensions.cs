using System.Linq;
using UnityEngine;

public static class TransformExtensions
{
    public static void DestroyChildren(this Transform trans)
    {
        foreach (var child in trans.Cast<Transform>().ToArray())
        {
            GameObject.Destroy(child.gameObject);
        }
    }

    public static void DestroyChildrenImmediately(this Transform trans)
    {
        foreach (var child in trans.Cast<Transform>().ToArray())
        {
            GameObject.DestroyImmediate(child.gameObject);
        }
    }

    public static void DestroyChildrenWithComponent<T>(this Transform trans) where T: MonoBehaviour
    {
        foreach (var child in trans.Cast<Transform>().ToArray())
        {
            if (child.GetComponent<T>() != null)
            {
                GameObject.DestroyImmediate(child.gameObject);
            }
        }
    }
}