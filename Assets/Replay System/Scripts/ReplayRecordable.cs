using System.Text.RegularExpressions;
using UnityEngine;

public class ReplayRecordable : MonoBehaviour
{
    [SerializeField] private bool _recordZAxis = true;
    [SerializeField] private bool _recordRotation = true;
    [SerializeField] private bool _recordScale = true;

    private static int _instanceCount = 0;

    private int _instanceID = 0;

    private void OnEnable()
    {
        _instanceCount += 1;
        _instanceID = _instanceCount;
    }

    internal ReplayObject GetReplayObject()
    {
        var obj = new ReplayObject()
        {
            name = TrimNumberInParentheses(name),
            id = _instanceID,
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