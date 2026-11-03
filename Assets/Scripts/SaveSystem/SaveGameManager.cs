using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public class SaveGameManager : MonoBehaviour
{
    public static SaveGameManager Instance { get; private set; } // crea una variable estatica de tipo SaveGameManager, osea que es esta misma clase, para que otros
                                                                 // scripts puedan acceder directamente a los metodos.

    [Header("Autosave")] // esto es simplemente un titulo/header que se muestra en el inspector
    [SerializeField] private bool autoSaveEnabled = true; // variable que idnica si el autoguardado esta activo, empieza con true
    [SerializeField, Min(1f)] private float autoSaveInterval = 30f; // variable que indica el intervalo de segundos que hace el autoSave

    private float autoSaveTimer; // esta variable cuenta el tiempo que ha pasado desde la última comprobación de guardado
    private bool isDirty; // indica si hay cambios pendientes para guardar

    private readonly Dictionary<string, ISaveable> objects = new Dictionary<string, ISaveable>(); // este diccionario guarda los objetos registrados para despues guardarlos y cargarlos,
                                                                                                  // el readonly es para que no se pueda asignar otro diccionario despues de iniciarlo,
                                                                                                  //pero se pueden meter y quitar elementos. para el objeto que se guarda utilizamos el 
                                                                                                  // ISaveable que contiene toda la información

    // Caché temporal en memoria: todavía no es el archivo definitivo.
    private readonly Dictionary<string, SaveData> pending = new Dictionary<string, SaveData>();

    private const int SaveFormatVersion = 1;
    private const int MaxObjects = 10000;

    public int PendingCount => pending.Count;
    public bool IsDirty => isDirty;

    // este indica que ruta del guardado de los archivos, devuelve un string, el path.combine combina laruta del archivo, osea que mete el archivo en la ruta de guardado
    public string SavePath => Path.Combine(Application.persistentDataPath, "seal_save.dat");

    // el awake principalmente sirve para que no se ejecuten mas de un gestor de guardado al mismo tiempo 
    private void Awake()
    {
        if (Instance != null && Instance != this) // si existe otro gestor, lo elimina
        {
            Destroy(gameObject);
            return;
        }

        Instance = this; // guarda el objeto que va a gestionar todo, que es el mismo
        DontDestroyOnLoad(gameObject); // esto hace que no se elimine el objeto al cambiar de escena
    }

    private void Update() // en resumen, el update controla el tiempo del autoSave 
    {
        if (!autoSaveEnabled) // si el autoSave esta desactivado,simplemente hacemos que el update deje de funcionar
            return;

        autoSaveTimer += Time.unscaledDeltaTime; // le suma el tiempo a la variable autoSaveTimer

        if (autoSaveTimer >= autoSaveInterval) // si el timer llega al tiempo indicado en la variable de intervalo, reinicia el timer y hace un guardado
        {
            autoSaveTimer = 0f;

            if (isDirty)
                SaveToDisk();
        }
    }


    public void Register(ISaveable saveable) // este es el metodo que va a registrar los objetos que se pueden guardar y añade sus datos iniciales a la caché temporal,
                                             // saveable es el objeto que vamos a registrar
    {
        if (saveable == null || string.IsNullOrWhiteSpace(saveable.SaveId)) // si el objeto no existe pues se cancela el metodo
        {
            Debug.LogWarning("SaveSystem: objeto o ID no válido.");
            return;
        }

        if (objects.TryGetValue(saveable.SaveId, out ISaveable existing)) // busca el identificador del objeto dentro del diccionario
        {
            if (ReferenceEquals(existing, saveable)) // si el bojeto esta duplicado, se cancela el registro
                return;

            Debug.LogError("SaveSystem: ID duplicado: " + saveable.SaveId);
            return;
        }

        objects.Add(saveable.SaveId, saveable); // añade el objeto al diccionario de objetos

        // Guardamos un estado inicial en la caché
        pending[saveable.SaveId] = saveable.CaptureState();
        isDirty = true; // indicamos que hay cambios pendientes por hacer

        Debug.Log("SaveSystem: registrado " + saveable.SaveId);
    }

    public void Unregister(ISaveable saveable)
    {
        if (saveable == null)  // si el objeto no existe salimos del metodo
			return;

		// Comprobamos que el objeto que queremos quitar sea el que esta registrado
		if (objects.TryGetValue(saveable.SaveId, out ISaveable registered) && ReferenceEquals(registered, saveable))
        {
            // Se elimina la referencia, pero no la caché, así podemos conservar datos aunque el objeto se destruya
            objects.Remove(saveable.SaveId);
        }
    }

    public void NotifyChanged(ISaveable saveable)
    {
        if (saveable == null) // comprobamos que el objeto exista
			return;

		// Comprobamos que el objeto este registrado correctamente
		if (!objects.TryGetValue(saveable.SaveId, out ISaveable registered) || !ReferenceEquals(registered, saveable))
        {
            Register(saveable);  // si no esta registrado, intentamos registrarlo

			// Volvemos a comprobar si se ha registrado correctamente
			if (!objects.TryGetValue(saveable.SaveId, out registered) || !ReferenceEquals(registered, saveable))
                return;
        }

        // Actualizamos la copia temporal, no el archivo
        pending[saveable.SaveId] = saveable.CaptureState();
        isDirty = true;

        Debug.Log("SaveSystem: cambio registrado en caché: " + saveable.SaveId + " | Pendientes: " + pending.Count);
    }
    
    public void SaveToDisk()
    {
        try
        {
			// Obtenemos la carpeta donde se guardara el archivo
			string directory = Path.GetDirectoryName(SavePath);

			// Si la carpeta no existe, la creamos
			if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

			// Creamos el archivo y utilizamos BinaryWriter para escribir los datos en binario
			using (FileStream stream = new FileStream(SavePath, FileMode.Create, FileAccess.Write)) 
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
				// Primero escribimos la version y el numero de objetos
				writer.Write(SaveFormatVersion);
                writer.Write(pending.Count);

				// Recorremos todos los objetos que tenemos en la cache temporal
				foreach (KeyValuePair<string, SaveData> entry in pending)
                {
                    SaveData data = entry.Value;

					// Escribimos el ID y el tipo del objeto
					writer.Write(data.id ?? "");
                    writer.Write(data.type ?? "");

					// Escribimos la posicion
					writer.Write(data.px);
                    writer.Write(data.py);
                    writer.Write(data.pz);

					// Escribimos la rotacion
					writer.Write(data.rx);
                    writer.Write(data.ry);
                    writer.Write(data.rz);

					// Escribimos si esta activo y su valor
					writer.Write(data.active);
                    writer.Write(data.value);
                }
            }

            isDirty = false; // indicamos que ya no hay cambios pendientes

			Debug.Log("SaveSystem: guardado correcto. Archivo: " + SavePath);
        }
        catch (Exception ex) // si ocurre algun error durante el guardado, lo capturamos
		{
            // Si falla el guardado, los cambios siguen pendientes
            Debug.LogError("SaveSystem: error al guardar: " + ex.Message);
        }
    }

    public void LoadFromDisk()
    {
        if (!File.Exists(SavePath)) // comprobamos si existe el archivo de guardado
		{
            Debug.LogWarning("SaveSystem: todavía no existe ningún archivo de guardado.");
            return;
        }

        try
        {
			// Creamos un diccionario temporal donde iremos guardando los datos leidos
			Dictionary<string, SaveData> loaded = new Dictionary<string, SaveData>();

            // Primero leemos el archivo completo, no modificamos los objetos mientras la lectura sea incompleta
            using (FileStream stream = new FileStream(SavePath, FileMode.Open, FileAccess.Read)) 
            using (BinaryReader reader = new BinaryReader(stream))

            {
                int version = reader.ReadInt32();  // leemos la version del archivo

				if (version != SaveFormatVersion) // comprobamos que la version sea compatible
					throw new InvalidDataException("Versión de archivo no compatible.");

                int count = reader.ReadInt32(); // leemos cuantos objetos hay guardados

				// Comprobamos que el numero de objetos sea valido
				if (count < 0 || count > MaxObjects)
                    throw new InvalidDataException("Número de objetos no válido.");

				// Recorremos todos los objetos guardados
				for (int i = 0; i < count; i++)
                {
                    SaveData data = new SaveData(); // creamos una estructura para guardar los datos leidos

					// Leemos el ID y el tipo
					data.id = reader.ReadString();
                    data.type = reader.ReadString();

					// Leemos la posicion
					data.px = reader.ReadSingle();
                    data.py = reader.ReadSingle();
                    data.pz = reader.ReadSingle();

					// Leemos la rotacion
					data.rx = reader.ReadSingle();
                    data.ry = reader.ReadSingle();
                    data.rz = reader.ReadSingle();

					// Leemos el estado activo y el valor
					data.active = reader.ReadBoolean();
                    data.value = reader.ReadInt32();

					// Comprobamos que el ID no este vacio
					if (string.IsNullOrWhiteSpace(data.id))
                        throw new InvalidDataException("Se ha encontrado un ID vacío.");

					// Comprobamos que no haya dos objetos con el mismo ID
					if (loaded.ContainsKey(data.id))
                        throw new InvalidDataException("ID duplicado en el archivo: " + data.id);

					// Añadimos los datos al diccionario temporal
					loaded.Add(data.id, data);
                }

				// Comprobamos que no haya datos adicionales en el archivo
				if (stream.Position != stream.Length)
                    throw new InvalidDataException("El archivo contiene datos adicionales inesperados.");
            }

            // Aplicamos los datos únicamente después de leer correctamente
            foreach (KeyValuePair<string, SaveData> entry in loaded)
            {
				// Buscamos el objeto que tenga el mismo ID que los datos guardados
				if (objects.TryGetValue(entry.Key, out ISaveable target))
                {
                    target.RestoreState(entry.Value); // aplicamos los datos guardados al objeto

					// La caché pasa a reflejar el estado restaurado
					pending[entry.Key] = target.CaptureState();
                }
                else
                    Debug.LogWarning("SaveSystem: no hay objeto registrado con ID " + entry.Key);
            }

            isDirty = false; // indicamos que no hay cambios pendientes despues de cargar

			Debug.Log("SaveSystem: carga completada. Registros leídos: " + loaded.Count);
        }
        catch (Exception ex)
        {
            Debug.LogError("SaveSystem: archivo inválido o error de carga: " + ex.Message);
        }
    }

    public void AutoSaveNow()
    {
		// Comprobamos si existen cambios pendientes
		if (isDirty)
            SaveToDisk();  // si hay cambios, realizamos el guardado
		else
            Debug.Log("SaveSystem: no hay cambios pendientes para autosave.");
    }

    public void SetAutoSave(bool enabled)
    {
        autoSaveEnabled = enabled; // activamos o desactivamos el autosave
		autoSaveTimer = 0f; // reiniciamos el temporizador


		Debug.Log("SaveSystem: autosave " + (enabled ? "activado." : "desactivado."));
    }

    public void SetAutoSaveInterval(float seconds)
    {
        autoSaveInterval = Mathf.Max(1f, seconds); // cambiamos el intervalo, con un minimo de un segundo
		autoSaveTimer = 0f; // reiniciamos el temporizador
	}

    public void DeleteSaveForTesting()
    {
        try
        {
			// Comprobamos si existe el archivo antes de eliminarlo
			if (File.Exists(SavePath))
                File.Delete(SavePath);

            Debug.Log("SaveSystem: archivo eliminado para la prueba.");
        }
        catch (Exception ex) // capturamos posibles errores al eliminar el archivo
		{
            Debug.LogError("SaveSystem: no se pudo eliminar el archivo: " + ex.Message);
        }
    }

    private void OnApplicationQuit()
    {
        // No guardamos automáticamente aquí: el guardado definitivo, depende de una orden de guardado o del autosave configurado
        if (Instance == this)
            Instance = null; // eliminamos la referencia al manager al cerrar la aplicación
	}
}