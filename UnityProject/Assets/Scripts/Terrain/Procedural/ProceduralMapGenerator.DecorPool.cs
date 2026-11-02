using System.Collections.Generic;
using UnityEngine;

// Decor instances: pooled per prefab and grouped under one parent per category.
public partial class ProceduralMapGenerator
{
    // Edit-mode preview decor must never end up serialized into the scene or prefab.
    private static GameObject MarkEditorOnly(GameObject root)
    {
        if (Application.isPlaying)
            return root;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            t.gameObject.hideFlags = HideFlags.DontSave;
        return root;
    }

    private Transform GetDecorRoot()
    {
        if (decorParent != null)
            return decorParent;
        decorParent = MarkEditorOnly(new GameObject(DecorRootName)).transform;
        decorParent.SetParent(transform, false);
        return decorParent;
    }

    // Category is the prefab name minus trailing digits (Tree1 -> Tree).
    private Transform GetDecorCategoryParent(GameObject prefab)
    {
        Transform root = GetDecorRoot();
        string category = System.Text.RegularExpressions.Regex.Replace(prefab.name, @"\d+$", "");
        Transform categoryParent;

        if (decorCategoryParents.TryGetValue(category, out categoryParent))
            return categoryParent;
        categoryParent = MarkEditorOnly(new GameObject(category)).transform;
        categoryParent.SetParent(root, false);
        decorCategoryParents[category] = categoryParent;
        return categoryParent;
    }

    // Instances get their category parent once at creation and keep it.
    private GameObject RentDecorInstance(GameObject prefab)
    {
        Stack<GameObject> pool;
        GameObject instance;

        if (decorPool.TryGetValue(prefab, out pool) && pool.Count > 0)
            return pool.Pop();
        instance = MarkEditorOnly(Instantiate(prefab, GetDecorCategoryParent(prefab)));
        if (Application.isPlaying && !instance.TryGetComponent(out RevealOccluder _))
            instance.AddComponent<RevealOccluder>();
        return instance;
    }

    private void ReturnToPool(GameObject instance, GameObject prefab)
    {
        Stack<GameObject> pool;

        instance.SetActive(false);
        if (!decorPool.TryGetValue(prefab, out pool))
        {
            pool = new Stack<GameObject>();
            decorPool[prefab] = pool;
        }
        pool.Push(instance);
    }

    // Pooled rather than destroyed, since chunks reload often.
    private void UnloadDecor(Vector2Int chunk)
    {
        List<(GameObject instance, GameObject prefab)> decorList;

        if (!decorObjects.Remove(chunk, out decorList))
            return;
        foreach ((GameObject instance, GameObject prefab) in decorList)
            ReturnToPool(instance, prefab);
    }
}
