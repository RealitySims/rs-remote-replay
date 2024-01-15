using System;
using System.Collections.Generic;

[Serializable]
public class ReplayFrame
{
    public float Time;
    public float LevelProgress;
    public float PlayerHealth;
    public float PlayerLevel;
    public ReplayObject Player;
    public ReplayObject[] Objects;
    public Dictionary<int, int> Upgrades = new Dictionary<int, int>();
}