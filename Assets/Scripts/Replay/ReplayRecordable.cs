using UnityEngine;

internal class ReplayRecordable : MonoBehaviour
{
    public static int _instanceCount = 0;

    public int _instanceID = 0;

    private void OnEnable()
    {
        _instanceCount += 1;
        _instanceID = _instanceCount;
    }

    internal ReplayObject GetReplayObject()
    {
        return new ReplayObject()
        {
            name = name.Replace("(Clone)", ""),
            id = _instanceID,
            Position = transform.position,
        };
    }
}