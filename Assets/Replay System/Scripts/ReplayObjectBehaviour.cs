using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ReplayObjectBehaviour : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _renderer = null;
    [SerializeField] private TMPro.TMP_Text _name = null;

    [SerializeField] private static Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>();

    public void Initialize(ReplayObject obj, bool useRealPrefab = false)
    {
        transform.position = obj.Position;

        GameObject prefab = GetPrefab(obj.name);
        var spriteRenderer = prefab?.GetComponentInChildren<SpriteRenderer>();
        if (useRealPrefab && prefab)
        {
            var instance = GameObject.Instantiate(prefab, transform);
            instance.transform.localPosition = Vector3.zero;

            foreach (var script in instance.GetComponentsInChildren<MonoBehaviour>())
            {
                script.enabled = false;
            }
            _renderer.enabled = false;
            _name.gameObject.SetActive(false);
        }
        else if (prefab && spriteRenderer)
        {
            _renderer.sprite = spriteRenderer.sprite;
            _renderer.transform.localScale = spriteRenderer.transform.lossyScale;
            _name.gameObject.SetActive(false);
        }
        else
        {
            _name.gameObject.SetActive(true);
            _name.SetText(obj.name);
        }
    }

    public static GameObject GetPrefab(string prefabName)
    {
        if (_prefabCache.ContainsKey(prefabName))
        {
            return _prefabCache[prefabName];
        }

#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets($"\"{prefabName}\"" + " t:GameObject");
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab != null)
            {
                // Check if the asset is actually a prefab and not a regular GameObject
                if (PrefabUtility.GetPrefabAssetType(prefab) != PrefabAssetType.NotAPrefab)
                {
                    _prefabCache[prefabName] = prefab;
                    return prefab;
                }
            }
        }
#endif
        return null;
    }

    internal void UpdatePosition(ReplayObject obj, float duration)
    {
        if (Vector3.Distance(obj.Position, transform.position) < 10)
        {
            transform.position = obj.Position;
            Debug.LogWarning("interpolation not implemented.");
        }
        else
        {
            transform.position = obj.Position;
        }
    }
}