using UnityEngine;

public class DemoInputController : MonoBehaviour
{
    [Header("Input Actions")]
    private SealTheDealControls controls;

    [Header("Objetos de la demo")]
    [SerializeField] private DemoSaveableObject player;
    [SerializeField] private DemoSaveableObject order;
    [SerializeField] private DemoSaveableObject door;

    [Header("Movimiento")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 4f;

    private void Awake()
    {
        controls = new SealTheDealControls();
    }

    private void OnEnable()
    {
        if (controls == null)
            return;

        controls.Gameplay.Enable();
    }

    private void OnDisable()
    {
        if (controls != null)
            controls.Gameplay.Disable();
    }

    private void OnDestroy()
    {
        if (controls != null)
            controls.Dispose();
    }

    private void Update()
    {
        if (controls == null)
            return;

        HandleMovement();
        HandleButtons();
    }

    private void HandleMovement()
    {
        if (player == null)
            return;

        Vector2 input = controls.Gameplay.Move.ReadValue<Vector2>();

        if (input.sqrMagnitude < 0.001f)
            return;

        input = Vector2.ClampMagnitude(input, 1f);

        Vector3 movement = new Vector3(input.x, 0f, input.y);

        player.MoveBy(movement * moveSpeed * Time.deltaTime);
    }

    private void HandleButtons()
    {
        if (controls.Gameplay.Save.WasPressedThisFrame())
            SaveGameManager.Instance?.SaveToDisk();

        if (controls.Gameplay.Load.WasPressedThisFrame())
            SaveGameManager.Instance?.LoadFromDisk();

        if (controls.Gameplay.ToggleDoor.WasPressedThisFrame())
            if (door != null)
                door.ToggleActive();

        if (controls.Gameplay.CollectOrder.WasPressedThisFrame())
            if (order != null)
                order.IncrementValue();

        if (controls.Gameplay.ResetDemo.WasPressedThisFrame())
            ResetDemo();
    }

    private void ResetDemo()
    {
        if (player != null)
            player.ResetToDefaults();

        if (order != null)
            order.ResetToDefaults();

        if (door != null)
            door.ResetToDefaults();

        Debug.Log("Demo: estado inicial restaurado. " + "Usa F5 para guardar si quieres conservarlo.");
    }
}