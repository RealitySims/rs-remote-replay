using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ReplayUpgradablesUI : MonoBehaviour
{

    [SerializeField] private GameObject _playerPrefab = null;

    private bool _initialized;
    private Dictionary<int, int> _previous = new Dictionary<int, int>();


    public void DrawFrame(Dictionary<int, int> upgrades)
    {
        bool areEqual = upgrades.Count == _previous.Count && !upgrades.Except(_previous).Any();
        if (areEqual && _initialized)
        {
            return;
        }
        _previous = upgrades;
        _initialized = true;

        transform.DestroyChildren();
    }
}