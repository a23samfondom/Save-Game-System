using UnityEngine;

public class DemoSaveableObject : MonoBehaviour, ISaveable
{
    [Header("Identidad de guardado")]
    [SerializeField] private string saveId = "replace_with_unique_id"; // ID unico que identifica al objeto

    [Header("Estado de demostración")]
    [SerializeField] private int value;  // valor de ejemplo que tambien vamos a guardar

    public string SaveId => saveId; // permite acceder al ID desde otros scripts
    public int Value => value; // permite consultar el valor actual


    private Vector3 initialPosition; // guarda la posicion inicial
    private Quaternion initialRotation; // guarda la rotacion inicial
    private bool initialActive; // guarda si el objeto empieza activo

    private void Awake()
    {
        // Guardamos los valores iniciales del objeto al empezar
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialActive = gameObject.activeSelf;
    }

    private void Start()
    {
        // Comprobamos que exista el manager antes de utilizarlo
        if (SaveGameManager.Instance == null)
        {
            Debug.LogError("SaveSystem: falta SaveGameManager en la escena.");
            return;
        }
        // Registramos este objeto en el sistema de guardado
        SaveGameManager.Instance.Register(this);
    }

    private void OnDestroy()
    {
        // Cuando se destruye el objeto, lo quitamos del registro
        if (SaveGameManager.Instance != null)
            SaveGameManager.Instance.Unregister(this);
    }

    public void MoveBy(Vector3 offset)
    {
        // Movemos el objeto sumando una distancia a su posicion actual
        transform.position += offset;
        // Avisamos al manager de que ha cambiado
        NotifyChange();
    }

    public void IncrementValue()
    {
        // Aumentamos el valor en uno
        value++;
        // Avisamos al manager para actualizar la caché
        NotifyChange();

        Debug.Log(saveId + " | Valor actual: " + value);
    }

    public void ToggleActive()
    {
        // Cambiamos el estado del objeto al contrario del que tiene actualmente
        SetActiveState(!gameObject.activeSelf);
    }

    public void SetActiveState(bool active)
    {
        // El manager conserva el registro aunque el objeto se desactive
        gameObject.SetActive(active);
        // Actualizamos el estado en la caché
        NotifyChange();
    }

    public void ResetToDefaults()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        value = 0;

        // Primero actualizamos el estado activo
        gameObject.SetActive(initialActive);
        NotifyChange();
    }

    public SaveData CaptureState()
    {
        SaveData data = new SaveData();

        data.id = saveId;
        data.type = GetType().Name;

        data.px = transform.position.x;
        data.py = transform.position.y;
        data.pz = transform.position.z;

        Vector3 rotation = transform.eulerAngles;

        data.rx = rotation.x;
        data.ry = rotation.y;
        data.rz = rotation.z;

        data.active = gameObject.activeSelf;
        data.value = value;

        return data;
    }

    public void RestoreState(SaveData data)
    {
        if (data == null)
            return;

        if (data.id != saveId)
        {
            Debug.LogWarning("SaveSystem: se intentó aplicar un ID incorrecto a " + saveId);
            return;
        }

        transform.position = new Vector3(data.px, data.py, data.pz);

        transform.eulerAngles = new Vector3(data.rx, data.ry, data.rz);

        value = data.value;

        // El componente puede desactivarse; la referencia del manager, se conserva porque no nos desregistramos en OnDisable
        gameObject.SetActive(data.active);
    }

    private void NotifyChange()
    {
        if (SaveGameManager.Instance != null)
            SaveGameManager.Instance.NotifyChanged(this);
    }
}