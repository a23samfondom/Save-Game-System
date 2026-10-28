public interface ISaveable // creamos una interfaz que se llama ISaveable, son como los requisitos que tiene el objeto para que se guarde
{
    string SaveId { get; } // obliga a tener una propiedad que pueda comprobar el id del objeto
    SaveData CaptureState(); // guarda todos los datos del objeto, este metodo es del script DemoSaveableObjects
    void RestoreState(SaveData data); // este metodo pilla los datos guardados y los aplica al objeto
}