using UnityEngine;

public class DemoSaveableObject : MonoBehaviour, ISaveable
{
    [Header("Identidad de guardado")]
    [SerializeField] private string saveId = "replace_with_unique_id";

    [Header("Estado de demostración")]
    [SerializeField] private int value;

    public string SaveId => saveId;
    public int Value => value;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private bool initialActive;

    private void Awake()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialActive = gameObject.activeSelf;
    }

    private void Start()
    {
        if (SaveGameManager.Instance == null)
        {
            Debug.LogError("SaveSystem: falta SaveGameManager en la escena.");
            return;
        }

        SaveGameManager.Instance.Register(this);
    }

    private void OnDestroy()
    {
        if (SaveGameManager.Instance != null)
            SaveGameManager.Instance.Unregister(this);
    }

    public void MoveBy(Vector3 offset)
    {
        transform.position += offset;
        NotifyChange();
    }

    public void IncrementValue()
    {
        value++;
        NotifyChange();

        Debug.Log(saveId + " | Valor actual: " + value);
    }

    public void ToggleActive()
    {
        SetActiveState(!gameObject.activeSelf);
    }

    public void SetActiveState(bool active)
    {
        // El manager conserva el registro aunque el objeto se desactive
        gameObject.SetActive(active);
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