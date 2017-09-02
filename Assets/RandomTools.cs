using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RandomTools {

    public static T[] GetComponentsInChildrenWithTag<T>(this GameObject gameObject, string tag)
        where T : Component
    {
        List<T> results = new List<T>();

        if (gameObject.CompareTag(tag))
            results.Add(gameObject.GetComponent<T>());

        foreach (Transform t in gameObject.transform)
            results.AddRange(t.gameObject.GetComponentsInChildrenWithTag<T>(tag));

        return results.ToArray();
    }

    public static T GetComponentInParents<T>(this GameObject gameObject)
        where T : Component
    {
        for (Transform t = gameObject.transform; t != null; t = t.parent)
        {
            T result = t.GetComponent<T>();
            if (result != null)
                return result;
        }

        return null;
    }

    public static T[] GetComponentsInParents<T>(this GameObject gameObject)
        where T : Component
    {
        List<T> results = new List<T>();
        for (Transform t = gameObject.transform; t != null; t = t.parent)
        {
            T result = t.GetComponent<T>();
            if (result != null)
                results.Add(result);
        }

        return results.ToArray();
    }

    public static Color WithAlpha(this Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
    }
    // GUI.color = desiredColor.WithAlpha(currentAlpha);

}
