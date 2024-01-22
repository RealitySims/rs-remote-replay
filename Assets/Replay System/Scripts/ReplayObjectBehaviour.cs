using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ReplayObjectBehaviour : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _renderer = null;
    [SerializeField] private TMPro.TMP_Text _name = null;

    [SerializeField] private static Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>();

    private Vector3 _initialPosition = Vector3.zero;
    private Vector3 _targetPosition = Vector3.zero;
    private float _lerpDuration;
    private float _lerpProgress = 1;

    private void LateUpdate()
    {
        if (_lerpProgress < 1)
        {
            _lerpProgress += Time.deltaTime / _lerpDuration;
            transform.position = Vector3.Lerp(_initialPosition, _targetPosition, _lerpProgress);
        }
    }

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
            CopySpriteRendererAttributes(spriteRenderer);
            _renderer.transform.localScale = spriteRenderer.transform.lossyScale;
            _name.gameObject.SetActive(false);
        }
        else
        {
            _name.gameObject.SetActive(true);
            _name.SetText(obj.name);
        }
    }

    private void CopySpriteRendererAttributes(SpriteRenderer spriteRenderer)
    {
        _renderer.sprite = spriteRenderer.sprite;
        _renderer.color = spriteRenderer.color;
        _renderer.drawMode = spriteRenderer.drawMode;
        _renderer.size = spriteRenderer.size;
        _renderer.tileMode = spriteRenderer.tileMode;
        _renderer.sortingOrder = spriteRenderer.sortingOrder;
        _renderer.sharedMaterial = spriteRenderer.sharedMaterial;
        _renderer.spriteSortPoint = spriteRenderer.spriteSortPoint;
    }

    public static GameObject GetPrefab(string prefabName)
    {
        if (_prefabCache.ContainsKey(prefabName))
        {
            return _prefabCache[prefabName];
        }

#if UNITY_EDITOR
        foreach (var prefab in FindAndSortPrefabs($"\"{prefabName}\"" + " t:GameObject"))
        {
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

    public static GameObject[] FindAndSortPrefabs(string searchTerm)
    {
#if UNITY_EDITOR
        // Find assets that match the search term
        string[] guids = AssetDatabase.FindAssets($"{searchTerm} t:GameObject");

        // Convert GUIDs to asset paths and then load the assets
        GameObject[] assets = guids.Select(guid => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid))).ToArray();

        // Sort the assets by name
        System.Array.Sort(assets, (a, b) => a.name.CompareTo(b.name));

        return assets;
#endif
        return null;
    }

    public void UpdatePosition(ReplayObject obj, float duration)
    {
        UpdatePosition(obj.Position, duration);
    }

    public void UpdatePosition(Vector3 cameraPosition, float duration)
    {
        if (Vector3.Distance(cameraPosition, transform.position) < 10)
        {
            _lerpDuration = duration;
            _lerpProgress = 0;
            _initialPosition = transform.position;
            _targetPosition = cameraPosition;
        }
        else
        {
            _lerpProgress = 1;
            transform.position = cameraPosition;
        }
    }
}