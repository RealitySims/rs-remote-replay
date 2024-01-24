using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[ExecuteInEditMode]
public class ReplayRecordable : MonoBehaviour
{
    [SerializeField] private bool _recordZAxis = true;
    [SerializeField] private bool _recordRotation = true;
    [SerializeField] private bool _recordScale = true;

    private static int _instanceCount = 0;
    private int _instanceID = 0;

    [SerializeField] private string _guid = "";
    [SerializeField] private string _prefabName = "";

    public string PrefabName => _prefabName;
    public string GUID => _guid;

    private bool _hasSynced = false;
    private bool _unsavedChangesToPrefab = false;
    private GameObject _prefab = null;

#if UNITY_EDITOR

    private void OnValidate()
    {
        _hasSynced = false;
        SyncWithPrefab();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            if (!_hasSynced)
            {
                SyncWithPrefab();
            }
            if (_hasSynced && !_unsavedChangesToPrefab && _prefab)
            {
                _unsavedChangesToPrefab = true;
                PrefabUtility.SavePrefabAsset(_prefab);
            }
        }
    }

    public void SyncWithPrefab()
    {
        GameObject prefabObject;
        string prefabPath;

        PrefabStage prefabStage = PrefabStageUtility.GetPrefabStage(gameObject);

        bool isOnPrefabStageObject;
        try
        {
            isOnPrefabStageObject = prefabStage && prefabStage.prefabContentsRoot == gameObject;
        }
        catch
        {
            return;
        }

        if (isOnPrefabStageObject)
        {
            prefabPath = prefabStage.assetPath;
            prefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }
        else
        {
            prefabObject = PrefabUtility.GetCorrespondingObjectFromOriginalSource(gameObject);
            prefabPath = AssetDatabase.GetAssetPath(prefabObject);
        }

        if (prefabPath != null && prefabPath != "")
        {
            _prefab = prefabObject;
            _hasSynced = true;
            _unsavedChangesToPrefab = false;

            _guid = AssetDatabase.AssetPathToGUID(prefabPath);
            _prefabName = prefabObject.name;
            EditorUtility.SetDirty(this);
            PrefabUtility.RecordPrefabInstancePropertyModifications(this);

            var component = prefabObject.GetComponent<ReplayRecordable>();
            component._guid = _guid;
            component._prefabName = _prefabName;
            EditorUtility.SetDirty(component);
            PrefabUtility.RecordPrefabInstancePropertyModifications(gameObject);
        }
    }
#endif

    private void OnEnable()
    {
        _instanceCount += 1;
        _instanceID = _instanceCount;
    }

    internal ReplayObject GetReplayObject()
    {
        var obj = new ReplayObject()
        {
            name = _prefabName,
            id = _instanceID,
            guid = _guid,
        };

        Vector3 pos = transform.position;
        obj.Position = _recordZAxis ? transform.position : new Vector3(pos.x, pos.y, 0);

        if (_recordRotation)
        {
            obj.Rotation = transform.eulerAngles;
        }

        if (_recordScale)
        {
            obj.Scale = transform.lossyScale;
        }

        return obj;
    }

    private static string TrimNumberInParentheses(string input)
    {
        string pattern = @"\([^)]*\)$";
        string result = Regex.Replace(input, pattern, "");
        return result.Trim();
    }
}