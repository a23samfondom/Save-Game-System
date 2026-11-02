using System;

[Serializable]
public class SaveData
{
    // Identificador del objeto y su tipo
    public string id;
    public string type;

    // Posición del objeto en los tres ejes
    public float px;
    public float py;
    public float pz;
    // Rotación del objeto en los tres ejes
    public float rx;
    public float ry;
    public float rz;
    // Indica si el objeto está activo y guarda su valor
    public bool active;
    public int value;
}