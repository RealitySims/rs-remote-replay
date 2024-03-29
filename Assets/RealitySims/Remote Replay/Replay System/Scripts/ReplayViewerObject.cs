using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ReplayViewerObject : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _renderer = null;
    private TilemapRenderer _tilemapRenderer = null;
    private Tilemap _tilemap = null;
    private Grid _grid = null;

    [SerializeField] private TMPro.TMP_Text _name = null;

    [SerializeField] private static Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>();

    private Vector3 _initialPosition = Vector3.zero;
    private Vector3 _targetPosition = Vector3.zero;
    private float _positionLerpDuration;
    private float _lerpProgress = 1;

    private Quaternion _initialRotation = Quaternion.identity;
    private Quaternion _targetRotation = Quaternion.identity;
    private float _rotationLerpDuration;
    private float _rotationLerpProgress;

    private void LateUpdate()
    {
        if (_lerpProgress < 1)
        {
            _lerpProgress += Time.deltaTime / _positionLerpDuration;
            transform.position = Vector3.Lerp(_initialPosition, _targetPosition, _lerpProgress);
        }

        if (_rotationLerpProgress < 1)
        {
            _rotationLerpProgress += Time.deltaTime / _rotationLerpDuration;
            transform.rotation = Quaternion.Lerp(_initialRotation, _targetRotation, _lerpProgress);
        }
    }

    public void Initialize(ReplayObject obj, bool useRealPrefab = false)
    {
        UpdateTransformation(obj, 0);
        transform.position = obj.Position;
        name = $"{obj.name}, {obj.id}";

        GameObject prefab = GetPrefab(obj.guid, obj.name);
        SpriteRenderer spriteRenderer = prefab?.GetComponentInChildren<SpriteRenderer>();
        TilemapRenderer tilemapRenderer = prefab?.GetComponentInChildren<TilemapRenderer>();
        Tilemap tilemap = prefab?.GetComponentInChildren<Tilemap>();
        Grid grid = prefab?.GetComponentInChildren<Grid>();

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
            _renderer.transform.localScale = GetRelativeScale(prefab.transform, spriteRenderer.transform);
            _renderer.transform.localPosition = GetRelativeOffset(prefab.transform, spriteRenderer.transform);
            _name.gameObject.SetActive(false);
        }
        else if (prefab && tilemapRenderer)
        {
            _renderer.enabled = false;
            var _gridGameObject = new GameObject();
            _gridGameObject.name = "Grid";
            _gridGameObject.transform.parent = this.transform;
            _grid = _gridGameObject.AddComponent<Grid>();
            var _tileGameObject = new GameObject();
            _tileGameObject.name = "Tiles";
            _tileGameObject.transform.parent = _gridGameObject.transform;
            _tilemap = _tileGameObject.AddComponent<Tilemap>();
            _tilemapRenderer = _tileGameObject.AddComponent<TilemapRenderer>();
            CopyTilemapRendererAttributes(grid, tilemapRenderer, tilemap);
            tilemapRenderer.transform.localScale = GetRelativeScale(prefab.transform, tilemapRenderer.transform);
            tilemapRenderer.transform.localPosition = GetRelativeOffset(prefab.transform, tilemapRenderer.transform);
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

    public static Vector3 GetRelativeScale(Transform ancestor, Transform child)
    {
        if (ancestor == child)
        {
            return Vector3.one;
        }

        Vector3 relativeScale = child.localScale;
        Transform currentParent = child.parent;

        // Traverse up the hierarchy until we reach the ancestor
        while (currentParent != null && currentParent != ancestor)
        {
            relativeScale = Vector3.Scale(relativeScale, currentParent.localScale);
            currentParent = currentParent.parent;
        }

        // If the loop exited without finding the ancestor, they are not in the same hierarchy
        if (currentParent != ancestor)
        {
            Debug.LogError("Ancestor is not a parent of the child");
            return Vector3.one;
        }

        return relativeScale;
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

    private void CopyTilemapRendererAttributes(Grid grid, TilemapRenderer renderer, Tilemap tilemap)
    {
        _grid.cellGap = grid.cellGap;
        _grid.cellSize = grid.cellSize;
        _grid.cellLayout = grid.cellLayout;
        grid.cellSwizzle = grid.cellSwizzle;

        _tilemap.tileAnchor = tilemap.tileAnchor;
        _tilemap.orientation = tilemap.orientation;
        _tilemapRenderer.sharedMaterial = renderer.sharedMaterial;
        _tilemapRenderer.sortingOrder = renderer.sortingOrder;
        _tilemapRenderer.sortOrder = renderer.sortOrder;
        _tilemapRenderer.mode = renderer.mode;
        _tilemapRenderer.detectChunkCullingBounds = renderer.detectChunkCullingBounds;
        _tilemapRenderer.chunkCullingBounds = renderer.chunkCullingBounds;
        _tilemapRenderer.maskInteraction = renderer.maskInteraction;
        _tilemapRenderer.sharedMaterial = renderer.sharedMaterial;

        _tilemap.animationFrameRate = tilemap.animationFrameRate;
        _tilemap.orientation = tilemap.orientation;

        // Get the bounds of the source tilemap
        BoundsInt bounds = tilemap.cellBounds;

        // Loop through all the cells in the source tilemap
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                // Get the cell position
                Vector3Int cellPosition = new Vector3Int(x, y, 0);
                // Get the tile from the source tilemap
                TileBase tile = tilemap.GetTile(cellPosition);
                // Set the tile to the destination tilemap
                _tilemap.SetTile(cellPosition, tile);
            }
        }
    }

    public static GameObject GetPrefab(string guid, string prefabName)
    {
        string key = guid + prefabName;
        if (_prefabCache.ContainsKey(key))
        {
            return _prefabCache[key];
        }

#if UNITY_EDITOR
        // Try to find by GUID
        var path = AssetDatabase.GUIDToAssetPath(guid);
        var obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        if (obj)
        {
            _prefabCache[key] = obj;
            return obj;
        }

        // Try name search
        foreach (var prefab in FindAndSortPrefabs(prefabName))
        {
            if (prefab != null)
            {
                // Check if the asset is actually a prefab and not a regular GameObject
                if (PrefabUtility.GetPrefabAssetType(prefab) != PrefabAssetType.NotAPrefab)
                {
                    _prefabCache[key] = prefab;
                    return prefab;
                }
            }
        }
#endif
        return null;
    }

#if UNITY_EDITOR
    public static GameObject[] FindAndSortPrefabs(string searchTerm)
    {
        // Find assets that match the search term
        string[] guids = AssetDatabase.FindAssets($"{searchTerm}" + " t:GameObject");

        // Convert GUIDs to asset paths and then load the assets
        GameObject[] assets = guids.Select(guid => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid))).ToArray();

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

        return assets;
    }
#endif

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

    public void UpdateTransformation(ReplayObject obj, float duration)
    {
        UpdatePosition(obj.Position, duration);
        UpdateRotation(obj.Rotation, duration);
        UpdateScale(obj.Scale);
    }

    public void UpdatePosition(Vector3 position, float duration)
    {
        if (duration > 0)
        {
            _positionLerpDuration = duration;
            _lerpProgress = 0;
            _initialPosition = transform.position;
            _targetPosition = position;
        }
        else
        {
            _lerpProgress = 1;
            transform.position = position;
        }
    }

    public void UpdateRotation(Vector3 eulerAngles, float duration)
    {
        if (duration > 0)
        {
            _rotationLerpDuration = duration;
            _rotationLerpProgress = 0;
            _initialRotation = transform.rotation;
            _targetRotation = Quaternion.Euler(eulerAngles);
        }
        else
        {
            _rotationLerpProgress = 1;
            transform.eulerAngles = eulerAngles;
        }
    }

    public void UpdateScale(Vector3 scale)
    {
        transform.localScale = scale;
    }
}