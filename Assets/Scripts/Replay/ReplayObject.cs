using Newtonsoft.Json;
using System;
using UnityEngine;

[Serializable]
public class ReplayObject
{
    public string name;
    public float x;
    public float y;
    public int id;

    [JsonIgnore]
    public Vector3 Position
    {
        get
        {
            return new Vector3(x, y, 0);
        }
        set
        {
            x = (float)Math.Round(value.x, 2);
            y = (float)Math.Round(value.y, 2);
        }
    }
}