using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;

public class ReplayRecordable : MonoBehaviour
{
    private static int _instanceCount = 0;

    private int _instanceID = 0;

    private void OnEnable()
    {
        _instanceCount += 1;
        _instanceID = _instanceCount;
    }

    internal ReplayObject GetReplayObject()
    {
        return new ReplayObject()
        {
            name = TrimNumberInParentheses(name),
            id = _instanceID,
            Position = transform.position,
        };
    }

    private static string TrimNumberInParentheses(string input)
    {
        string pattern = @"\([^)]*\)$";
        string result = Regex.Replace(input, pattern, "");
        return result.Trim();
    }
}