using Newtonsoft.Json;
using System;
using System.ComponentModel;
using UnityEngine;

[Serializable]
public class ReplayObject
{
    public string name;
    public int id;

    // Position
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public float x;

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public float y;

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public float z;

    // Rotation
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public float xR;

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public float yR;

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public float zR;

    // Scale
    [DefaultValue(1f)]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public float xS = 1f;

    [DefaultValue(1f)]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public float yS = 1f;

    [DefaultValue(1f)]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public float zS = 1f;

    [JsonIgnore]
    public Vector3 Position
    {
        get
        {
            return new Vector3(x, y, z);
        }
        set
        {
            x = (float)Math.Round(value.x, 2);
            y = (float)Math.Round(value.y, 2);
            z = (float)Math.Round(value.z, 2);
        }
    }

    [JsonIgnore]
    public Vector3 Rotation
    {
        get
        {
            return new Vector3(xR, yR, zR);
        }
        set
        {
            xR = (float)Math.Round(value.x, 2);
            yR = (float)Math.Round(value.y, 2);
            zR = (float)Math.Round(value.z, 2);
        }
    }

    [JsonIgnore]
    public Vector3 Scale
    {
        get
        {
            return new Vector3(xS, yS, zS);
        }
        set
        {
            xS = (float)Math.Round(value.x, 2);
            yS = (float)Math.Round(value.y, 2);
            zS = (float)Math.Round(value.z, 2);
        }
    }
}