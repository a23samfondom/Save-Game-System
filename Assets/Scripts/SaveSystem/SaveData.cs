using System;

[Serializable]
public class SaveData
{
    public string id;
    public string type;

    public float px;
    public float py;
    public float pz;

    public float rx;
    public float ry;
    public float rz;

    public bool active;
    public int value;
}