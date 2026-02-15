using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

[RequireComponent(typeof(EventSystem))]
public class UIInputModuleRebinder : MonoBehaviour
{
#if ENABLE_INPUT_SYSTEM
    [Tooltip("Optional. If empty, uses the module's current Actions Asset.")]
    [SerializeField] private InputActionAsset actionsAssetOverride;

    private void Awake()
    {
        var module = GetComponent<InputSystemUIInputModule>();
        if (module == null)
            return;

        var asset = actionsAssetOverride != null ? actionsAssetOverride : module.actionsAsset;
        if (asset == null)
            return;

        module.actionsAsset = asset;

        // Rebind by action path so stale serialized action references don't kill UI clicks.
        module.point = GetRef(asset, "UI/Point");
        module.move = GetRef(asset, "UI/Navigate");
        module.submit = GetRef(asset, "UI/Submit");
        module.cancel = GetRef(asset, "UI/Cancel");
        module.leftClick = GetRef(asset, "UI/Click");
        module.rightClick = GetRef(asset, "UI/RightClick");
        module.middleClick = GetRef(asset, "UI/MiddleClick");
        module.scrollWheel = GetRef(asset, "UI/ScrollWheel");
        module.trackedDevicePosition = GetRef(asset, "UI/TrackedDevicePosition");
        module.trackedDeviceOrientation = GetRef(asset, "UI/TrackedDeviceOrientation");
    }

    private static InputActionReference GetRef(InputActionAsset asset, string path)
    {
        var action = asset.FindAction(path, true);
        return action != null ? InputActionReference.Create(action) : null;
    }
#endif
}
