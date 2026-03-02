using UnityEngine;
using UnityEngine.InputSystem;

public class MenuInputManager : MonoBehaviour
{
    public static MenuInputManager instance;

    [SerializeField] private InputActionReference menuOpenCloseAction;

    public bool MenuOpenCloseInput { get; private set; }


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }


    }

    private void OnEnable()
    {
        menuOpenCloseAction.action.Enable();
    }

    private void OnDisable()
    {
        menuOpenCloseAction.action.Disable();
    }

    private void Update()
    {
        MenuOpenCloseInput = menuOpenCloseAction.action.WasPressedThisFrame();
    }
}
