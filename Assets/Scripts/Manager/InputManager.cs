// InputManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

public class InputManager : MonoBehaviour
{
    public static InputManager instance;

    private Dictionary<InputDevice, GameObject> deviceToPlayerMap = new Dictionary<InputDevice, GameObject>();

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    /// <summary>
    /// 为指定玩家分配设备和控制方案
    /// </summary>
    public void AssignDeviceToPlayer(Player player, InputDevice device, string controlScheme)
    {
        PlayerInput pi = player.GetComponent<PlayerInput>();
        if (pi == null) return;

        // 先解除其他玩家对该设备的绑定
        foreach (var kv in deviceToPlayerMap)
        {
            if (kv.Key == device) continue;
            PlayerInput otherPi = kv.Value.GetComponent<PlayerInput>();
            if (otherPi != null && otherPi.user.valid)
                otherPi.user.UnpairDevice(device);
        }

        pi.SwitchCurrentControlScheme(controlScheme, device);
        deviceToPlayerMap[device] = player.gameObject;
        Debug.Log($"为玩家 {player.name} 分配设备 {device.displayName} 方案 {controlScheme}");
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.Removed)
        {
            if (deviceToPlayerMap.TryGetValue(device, out GameObject player))
            {
                // 设备移除，可以处理玩家失去控制等
                deviceToPlayerMap.Remove(device);
            }
        }
    }
    public void AssignDeviceToController(PlayerInput controllerInput, InputDevice device, string controlScheme)
    {
        if (controllerInput == null) return;
        controllerInput.SwitchCurrentControlScheme(controlScheme, device);
    }
}