using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

public class InputManager : MonoBehaviour
{
    public static InputManager instance;

    public GameObject playerPrefab;

    // 记录已绑定玩家的设备
    private Dictionary<InputDevice, GameObject> deviceToPlayerMap = new Dictionary<InputDevice, GameObject>();

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        DontDestroyOnLoad(gameObject.transform.root.gameObject);
    }

    #region 111
    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    private void Update()
    {
        // 检测键盘 Y 键
        if (Keyboard.current != null && Keyboard.current.yKey.wasPressedThisFrame)
        {
            TryCreatePlayerForDevice(Keyboard.current);
        }

        // 检测所有手柄的 Y 按钮（buttonNorth）
        foreach (var gamepad in Gamepad.all)
        {
            if (gamepad.buttonNorth.wasPressedThisFrame)
            {
                TryCreatePlayerForDevice(gamepad);
            }
        }

        if (Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame)
        {
            Debug.Log("=== 当前玩家绑定信息 ===");
            foreach (var pi in PlayerInput.all)
            {
                if (pi == null) continue;
                var devices = string.Join(", ", pi.devices.Select(d => d.displayName));
                Debug.Log($"{pi.gameObject.name} 控制方案: {pi.currentControlScheme}, 设备: {devices}");
            }
        }
    }

    private void TryCreatePlayerForDevice(InputDevice device)
    {
        if (!deviceToPlayerMap.ContainsKey(device))
        {
            CreatePlayerForDevice(device);
        }
    }

    private void CreatePlayerForDevice(InputDevice device)
    {
        if (deviceToPlayerMap.ContainsKey(device)) return;
        if (playerPrefab == null)
        {
            Debug.LogError("playerPrefab 未赋值！");
            return;
        }

        // 1. 先解除其他玩家对该设备的绑定（避免输入被多个玩家共享）
        foreach (var pi in PlayerInput.all.Where(p => p != null))
        {
            if (pi.user.valid)
            {
                pi.user.UnpairDevice(device);
            }
        }

        // 2. 根据设备类型决定控制方案（如果预制体里的 Input Action Asset 中包含该方案）
        string controlScheme = GetControlSchemeForDevice(device);
        var prefabPlayerInput = playerPrefab.GetComponent<PlayerInput>();
        var asset = prefabPlayerInput?.actions;

        bool hasScheme = !string.IsNullOrEmpty(controlScheme) &&
                         asset != null &&
                         asset.controlSchemes.Any(s => s.name == controlScheme);

        // 3. 实例化玩家，如果控制方案存在则传入，否则让系统自动匹配
        PlayerInput newPlayerInput;
        if (hasScheme)
        {
            newPlayerInput = PlayerInput.Instantiate(playerPrefab,
                                                      controlScheme: controlScheme,
                                                      pairWithDevice: device);
        }
        else
        {
            if (!string.IsNullOrEmpty(controlScheme))
            {
                Debug.LogWarning($"控制方案 '{controlScheme}' 在 Action Asset 中不存在，将使用自动匹配。");
            }
            newPlayerInput = PlayerInput.Instantiate(playerPrefab, pairWithDevice: device);
        }

        if (newPlayerInput == null)
        {
            Debug.LogError("PlayerInput.Instantiate 失败！");
            return;
        }

        // 4. 记录映射关系
        GameObject newPlayer = newPlayerInput.gameObject;
        newPlayer.name = $"Player_{device.displayName}";
        deviceToPlayerMap[device] = newPlayer;

        Debug.Log($"已为设备 {device.displayName} 创建玩家，绑定的设备：{string.Join(",", newPlayerInput.devices.Select(d => d.displayName))}");
    }

    // 调试：打印当前所有 PlayerInput 的绑定设备信息
    private void LogAllPlayerInputBindings(string tag = null)
    {
        try
        {
            Debug.Log($"[InputManager] PlayerInput binding dump {tag ?? ""} -- count: {PlayerInput.all.Count}");
            foreach (var pi in PlayerInput.all)
            {
                if (pi == null) continue;
                var names = "";
                try { names = string.Join(",", pi.devices.Select(d => d.displayName)); } catch { names = "(error)"; }
                var userInfo = "";
                try { userInfo = pi.user.valid ? string.Join(",", pi.user.pairedDevices.Select(d => d.displayName)) : "user.invalid"; } catch { userInfo = "(user error)"; }
                Debug.Log($"[InputManager] PlayerInput: {pi.gameObject.name} | devices: {names} | user.paired: {userInfo}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"LogAllPlayerInputBindings failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 根据设备类型返回对应的控制方案名称
    /// </summary>
    private string GetControlSchemeForDevice(InputDevice device)
    {
        if (device is Gamepad) return "Gamepad";
        if (device is Keyboard) return "Keyboard";
        // 可根据需要添加鼠标、摇杆等
        return null;
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.Removed)
        {
            if (deviceToPlayerMap.TryGetValue(device, out GameObject player))
            {
                Destroy(player);
                deviceToPlayerMap.Remove(device);
            }
        }

        if (change == InputDeviceChange.Added)
        {
            Debug.Log($"新设备加入: {device.displayName}");
            // 直接尝试为该设备创建玩家
            TryCreatePlayerForDevice(device);
        }
    }

    #endregion
}