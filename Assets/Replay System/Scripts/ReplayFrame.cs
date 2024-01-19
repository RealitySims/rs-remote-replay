using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ReplayFrame
{
    public float Time;
    public float LevelProgress;
    public float PlayerHealth;
    public float PlayerLevel;
    public ReplayObject Camera;
    public ReplayObject[] Objects;
    public Dictionary<int, int> Upgrades = new Dictionary<int, int>();
}