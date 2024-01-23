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
        name = $"{obj.name}, {obj.id}";

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
            _renderer.transform.localPosition = GetRelativeOffset(prefab.transform, spriteRenderer.transform);
            _name.gameObject.SetActive(false);
        }
        else
        {
            _name.gameObject.SetActive(true);
            _name.SetText(obj.name);
        }
    }

    public static Vector3 GetRelativeOffset(Transform ancestor, Transform child)
    {
        if (ancestor == null || child == null)
        {
            Debug.LogError("Ancestor or Child is null");
            return Vector3.zero;
        }

        // Convert child's world position to ancestor's local position
        return ancestor.InverseTransformPoint(child.position);
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
        foreach (var prefab in FindAndSortPrefabs(prefabName))
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
        // Find assets that match the search term
        string[] guids = AssetDatabase.FindAssets($"{searchTerm}" + " t:GameObject");

        // Convert GUIDs to asset paths and then load the assets
        GameObject[] assets = guids.Select(guid => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid))).ToArray();

        Debug.LogWarning(searchTerm);
        // Sort the assets based on relevance to the search term
        System.Array.Sort(assets, (a, b) =>
        {
            int aScore = CalculateRelevanceScore(a.name, searchTerm);
            int bScore = CalculateRelevanceScore(b.name, searchTerm);
            //Debug.Log($"{a.name} {aScore} {b.name} {bScore} {searchTerm}");

            if (aScore == bScore)
            {
                return a.name.CompareTo(b.name); // If scores are equal, sort alphabetically
            }

            return bScore.CompareTo(aScore); // Higher score first
        });

        foreach (var term in assets)
        {
            Debug.Log(term.name);
        }

        return assets;
    }

    private static int CalculateRelevanceScore(string name, string searchTerm)
    {
        if (name.Equals(searchTerm, System.StringComparison.OrdinalIgnoreCase))
            return 3; // Highest score for exact match
        if (name.StartsWith(searchTerm, System.StringComparison.OrdinalIgnoreCase))
            return 2; // Medium score for starting with the search term
        if (name.Contains(searchTerm))
            return 1; // Lower score for containing the search term

        return 0; // No match
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