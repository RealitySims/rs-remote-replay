using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ReplayFrame
{
    public float Time;

    public ReplayObject Camera;
    public ReplayObject[] Objects;

    public Dictionary<string, string> Stats;
}