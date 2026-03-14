using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.Users;

public class InputManager : MonoBehaviour
{
    public static InputManager instance { get; private set; }

    [Header("Settings")]
    public GameObject playerPrefab;          // 玩家预制体（需包含 PlayerInput 和 Player 脚本）
    public int maxPlayers = 2;                // 最大玩家数

    private PlayerControl playerControl;      // 生成的输入包装类
    private Dictionary<InputDevice, HashSet<string>> deviceActiveMaps = new(); // 设备 -> 已激活的地图名集合
    private Dictionary<InputDevice, List<GameObject>> devicePlayersMap = new(); // 设备 -> 该设备控制的玩家列表

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        // 完成单例初始化
        instance = this;
        DontDestroyOnLoad(gameObject.transform.root.gameObject);
        playerControl = new PlayerControl();
    }

    private void Update()
    {
        // 手动轮询输入并路由到 player1/player2 的 externalMoveInput，避免 InputAction 映射冲突
        try
        {
            var players = PlayerInput.all.Select(pi => pi == null ? null : pi.GetComponent<Player>()).Where(p => p != null).ToList();
            var p1 = players.FirstOrDefault(p => p.playerIndex == 1);
            var p2 = players.FirstOrDefault(p => p.playerIndex == 2);

            // 回退查找：若索引未正确设置，尝试根据 controlScheme 或绑定设备推断
            if (p1 == null)
            {
                p1 = players.FirstOrDefault(p => p.controlScheme == "KeyboardWASD" || (p.boundDevice is Keyboard && p.controlScheme == "KeyboardWASD"));
            }
            if (p2 == null)
            {
                p2 = players.FirstOrDefault(p => p.controlScheme == "KeyboardArrows" || p.controlScheme == "Gamepad" || (p.boundDevice is Gamepad) || (p.boundDevice is Keyboard && p.controlScheme == "KeyboardArrows"));
            }

            // 最后回退：如果仍未找到 p1/p2，按创建顺序分配（第一个为 p1，第二个为 p2）
            if (p1 == null && players.Count > 0)
                p1 = players[0];
            if (p2 == null && players.Count > 1)
                p2 = players.FirstOrDefault(p => p != null && p != p1);

            var kb = Keyboard.current;
            Debug.Log($"[InputManager] p1={p1?.name}:{p1?.playerIndex} p2={p2?.name}:{p2?.playerIndex} bound2={(p2?.boundDevice!=null?p2.boundDevice.displayName:"null")}");
            if (p1 != null && kb != null)
            {
                float x = (kb.dKey.isPressed ? 1f : 0f) + (kb.aKey.isPressed ? -1f : 0f);
                float y = (kb.wKey.isPressed ? 1f : 0f) + (kb.sKey.isPressed ? -1f : 0f);
                p1.externalMoveInput = new UnityEngine.Vector2(x, y);
                if (x != 0f || y != 0f)
                    Debug.Log($"[InputManager] p1 externalMoveInput set to {p1.externalMoveInput}");
            }

            if (p2 != null)
            {
                // 同时读取手柄与箭头输入：箭头优先（若按下），否则使用手柄
                Vector2 gpV = Vector2.zero;
                try
                {
                    if (p2.boundDevice is Gamepad)
                    {
                        var gp = p2.boundDevice as Gamepad;
                        if (gp == null && Gamepad.all.Count > 0) gp = Gamepad.all[0];
                        if (gp != null) gpV = gp.leftStick.ReadValue();
                    }
                    else if (Gamepad.all.Count > 0)
                    {
                        // 如果 boundDevice 不是 Gamepad，但有手柄连接，也读取第一个手柄作为备选
                        gpV = Gamepad.all[0].leftStick.ReadValue();
                    }
                }
                catch { }

                Vector2 arrowsV = Vector2.zero;
                try
                {
                    if (kb != null)
                    {
                        float ax = (kb.rightArrowKey.isPressed ? 1f : 0f) + (kb.leftArrowKey.isPressed ? -1f : 0f);
                        float ay = (kb.upArrowKey.isPressed ? 1f : 0f) + (kb.downArrowKey.isPressed ? -1f : 0f);
                        arrowsV = new UnityEngine.Vector2(ax, ay);
                    }
                }
                catch { }

                // 箭头优先覆盖手柄
                var finalV = arrowsV != Vector2.zero ? arrowsV : gpV;
                p2.externalMoveInput = finalV;
                if (finalV != Vector2.zero)
                    Debug.Log($"[InputManager] p2.externalMoveInput = {finalV} (arrows={arrowsV}, gamepad={gpV})");

                // 为 KeyboardArrows 提供跳跃支持（因为我们频繁对键盘做手动路由），
                // 当检测到上箭头在这一帧被按下且玩家在地面时，直接设置其垂直速度实现跳跃。
                try
                {
                    if (kb != null && kb.upArrowKey.wasPressedThisFrame && p2 != null)
                    {
                        try
                        {
                            if (p2.IsGroundDetected())
                            {
                                p2.rb.velocity = new Vector2(p2.rb.velocity.x, p2.jumpForce);
                                Debug.Log($"[InputManager] p2 jump applied via arrow key, jumpForce={p2.jumpForce}");
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }
        }
        catch { }
    }
    

    private void Start()
    {
        // 创建默认玩家：player1 使用 WASD
        if (Keyboard.current != null)
        {
            TryCreatePlayer(Keyboard.current, "KeyboardWASD");
        }

        // player2 优先使用第一个手柄，否则使用方向键
        if (maxPlayers >= 2)
        {
            if (Gamepad.all.Count > 0)
            {
                TryCreatePlayer(Gamepad.all[0], "Gamepad");
            }
            else if (Keyboard.current != null)
            {
                // 不直接创建 KeyboardArrows 的 PlayerInput，以避免与 WASD 直接冲突。
                // 改为延迟到玩家按下箭头键时创建（通过 OnArrowsJoin 处理）。
                // TryCreatePlayer(Keyboard.current, "KeyboardArrows");
            }
        }
    }

    private void OnEnable()
    {
        playerControl.Enable();

        // 注册加入事件
        playerControl.Gamepad.Set.performed += OnGamepadJoin;          // 手柄 Y 按钮
        playerControl.KeyboardWASD.Move.performed += OnWASDJoin;      // WASD 任意键
        playerControl.KeyboardArrows.Move.performed += OnArrowsJoin;  // 方向键任意键

        InputSystem.onDeviceChange += OnDeviceChanged;
    }

    private void OnDisable()
    {
        playerControl.Gamepad.Set.performed -= OnGamepadJoin;
        playerControl.KeyboardWASD.Move.performed -= OnWASDJoin;
        playerControl.KeyboardArrows.Move.performed -= OnArrowsJoin;

        InputSystem.onDeviceChange -= OnDeviceChanged;

        playerControl.Disable();
    }

    private void OnGamepadJoin(InputAction.CallbackContext context)
    {
        if (context.control.device is Gamepad gamepad)
            TryCreatePlayer(gamepad, "Gamepad");
    }

    private void OnWASDJoin(InputAction.CallbackContext context)
    {
        if (context.control.device is Keyboard keyboard)
            TryCreatePlayer(keyboard, "KeyboardWASD");
    }

    private void OnArrowsJoin(InputAction.CallbackContext context)
    {
        if (context.control.device is Keyboard keyboard)
        {
            // 如果已有手柄被分配给 player2，则箭头键不应创建 player2（手柄优先）
            bool gamepadAssignedToPlayer2 = PlayerInput.all.Any(pi =>
            {
                if (pi == null) return false;
                var p = pi.GetComponent<Player>();
                return p != null && p.playerIndex == 2 && p.boundDevice is Gamepad;
            });

            if (gamepadAssignedToPlayer2)
            {
                Debug.Log("忽略箭头键：已有手柄控制 player2（手柄优先）");
                return;
            }

            // 创建 player2（箭头），但只在尚未有 player2 的情况下
            if (!IsPlayerIndexTaken(2))
            {
                TryCreatePlayer(keyboard, "KeyboardArrows");
            }
            else
            {
                // 如果 player2 已存在，但是由手柄创建的，我们需要把箭头输入路由给该 player2 的 externalMoveInput
                var player2 = PlayerInput.all.Select(pi => pi.GetComponent<Player>()).FirstOrDefault(p => p != null && p.playerIndex == 2);
                if (player2 != null)
                {
                    // 读取当前箭头方向并写入 externalMoveInput
                    Vector2 arrows = Vector2.zero;
                    try
                    {
                        var keyb = Keyboard.current;
                        if (keyb != null)
                        {
                            float x = (keyb.rightArrowKey.isPressed ? 1f : 0f) + (keyb.leftArrowKey.isPressed ? -1f : 0f);
                            float y = (keyb.upArrowKey.isPressed ? 1f : 0f) + (keyb.downArrowKey.isPressed ? -1f : 0f);
                            arrows = new Vector2(x, y);
                        }
                    }
                    catch { }

                    try { player2.externalMoveInput = arrows; } catch { }
                }
            }
        }
    }

    private void TryCreatePlayer(InputDevice device, string actionMapName)
    {
        // 玩家数已达上限
        if (PlayerInput.all.Count >= maxPlayers)
        {
            Debug.Log($"已达最大玩家数 ({maxPlayers})，无法创建新玩家");
            return;
        }

        // 检查该设备是否已激活该地图（防止重复创建）
        if (deviceActiveMaps.TryGetValue(device, out var maps))
        {
            if (maps.Contains(actionMapName))
            {
                Debug.Log($"设备 {device.displayName} 已激活地图 {actionMapName}，忽略");
                return;
            }
        }
        else
        {
            deviceActiveMaps[device] = new HashSet<string>();
        }

        // 检查玩家索引是否被占用（玩家1 = WASD，玩家2 = 方向键/手柄）
        if (actionMapName == "KeyboardWASD" && IsPlayerIndexTaken(1))
        {
            Debug.Log("玩家1 (WASD) 已存在，无法创建");
            return;
        }
        if ((actionMapName == "KeyboardArrows" || actionMapName == "Gamepad") && IsPlayerIndexTaken(2))
        {
            Debug.Log("玩家2 (方向键/手柄) 已存在，无法创建");
            return;
        }

        // 允许在同一物理键盘上创建多个玩家（WASD 与 箭头 使用不同的绑定），但会为每个玩家克隆 actions 以避免共享状态冲突

        // 手柄设备：先解除与其他玩家的绑定，确保设备独占
        if (device is Gamepad)
        {
            foreach (var pi in PlayerInput.all.Where(p => p != null))
            {
                if (pi.user.valid)
                    pi.user.UnpairDevice(device);
            }
        }

        // 实例化玩家，并配对设备
        PlayerInput newPlayerInput = PlayerInput.Instantiate(playerPrefab, pairWithDevice: device);
        if (newPlayerInput == null)
        {
            Debug.LogError("PlayerInput.Instantiate 失败！");
            return;
        }

        // 立即设置 Player 脚本的基础信息，避免 Player.Start 在未设置索引前运行导致 index 为 0
        try
        {
            var earlyPlayer = newPlayerInput.GetComponent<Player>();
            if (earlyPlayer != null)
            {
                earlyPlayer.boundDevice = device;
                earlyPlayer.controlScheme = actionMapName;
                earlyPlayer.playerInput = newPlayerInput;
                earlyPlayer.playerIndex = actionMapName == "KeyboardWASD" ? 1 : 2;
            }
        }
        catch { }

        // 克隆 actions asset 确保 WASD/Arrows 在同一键盘上不会互相影响
        try
        {
            if (newPlayerInput.actions != null)
            {
                var cloned = UnityEngine.Object.Instantiate(newPlayerInput.actions);
                newPlayerInput.actions = cloned;
                // 对于 KeyboardArrows（同一键盘上的第二玩家），不要让 PlayerInput 直接监听键盘，
                // 我们将在 InputManager.Update 中手动读取箭头按键并写入 player2 的 moveInput，避免与 WASD 冲突
                try
                {
                    // 将此 PlayerInput 的 actions 限定为配对的设备，确保 action map 能接收该设备事件
                    try { newPlayerInput.actions.devices = new ReadOnlyArray<InputDevice>(new[] { device }); } catch { }
                }
                catch { }
            }
        }
        catch { }

        // 手动激活对应的 Action Map
        if (!string.IsNullOrEmpty(actionMapName) && newPlayerInput.actions != null)
        {
            var map = newPlayerInput.actions.FindActionMap(actionMapName);
            if (map != null)
            {
                // 先禁用所有地图，再显式启用目标地图，确保动作被启用
                try
                {
                    foreach (var m in newPlayerInput.actions.actionMaps)
                        m.Disable();
                }
                catch { }

                map.Enable();
                // 确保 map 内的所有动作也被启用
                try
                {
                    foreach (var a in map.actions)
                        a.Enable();
                }
                catch { }
                // 如果是键盘映射，确保在同一 keyboard 的其它 PlayerInput 上禁用相反的键盘映射，避免互相抢占
                try
                {
                    if (device is Keyboard)
                    {
                        string otherMapName = null;
                        if (actionMapName == "KeyboardWASD") otherMapName = "KeyboardArrows";
                        else if (actionMapName == "KeyboardArrows") otherMapName = "KeyboardWASD";

                        if (!string.IsNullOrEmpty(otherMapName))
                        {
                            // 在新创建的 PlayerInput 上禁用另一个键盘地图（如果存在）
                            try
                            {
                                var other = newPlayerInput.actions.FindActionMap(otherMapName);
                                if (other != null) other.Disable();
                            }
                            catch { }

                            // 在已有的 PlayerInput 实例上，禁用本次要启用的地图（actionMapName），
                            // 确保已有玩家不会响应新玩家的按键映射
                            foreach (var pi in PlayerInput.all)
                            {
                                if (pi == null || pi == newPlayerInput) continue;
                                try
                                {
                                    if (pi.user.valid && pi.user.pairedDevices.Contains(device))
                                    {
                                        var mapToDisableOnExisting = pi.actions?.FindActionMap(actionMapName);
                                        if (mapToDisableOnExisting != null)
                                            mapToDisableOnExisting.Disable();
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }
                catch { }
                // 切换 PlayerInput 当前地图（有助于 PlayerInput.currentActionMap 返回正确值）
                try { newPlayerInput.SwitchCurrentActionMap(actionMapName); } catch { }

                // 额外尝试将设备与新玩家的 InputUser 配对
                try
                {
                    var user = newPlayerInput.user;
                    if (user.valid)
                    {
                        UnityEngine.InputSystem.Users.InputUser.PerformPairingWithDevice(device, user);
                    }
                }
                catch { }

                // 不对 actions.devices 进行全局修改（会影响整个 asset），仅确保该玩家的 action map 和 actions 启用
                try { newPlayerInput.actions.Enable(); } catch { }

                // 如果 actionMapName 是 KeyboardArrows 或 Gamepad，把 playerScript.playerIndex 设为 2 确保 PlayerMoveState 正确识别
                try
                {
                    var ps = newPlayerInput.GetComponent<Player>();
                    if (ps != null)
                    {
                        if (actionMapName == "KeyboardArrows" || actionMapName == "Gamepad")
                            ps.playerIndex = 2;
                        else if (actionMapName == "KeyboardWASD")
                            ps.playerIndex = 1;
                    }
                }
                catch { }

                Debug.Log($"玩家 {newPlayerInput.gameObject.name} 切换到地图 {actionMapName}");
            }
            else
            {
                Debug.LogError($"未找到地图 {actionMapName}，销毁无效玩家");
                Destroy(newPlayerInput.gameObject);
                return;
            }
        }

        // 设置玩家脚本中的信息
        Player playerScript = newPlayerInput.GetComponent<Player>();
        if (playerScript != null)
        {
            Debug.Log(111);
            playerScript.boundDevice = device;
            playerScript.controlScheme = actionMapName;
            playerScript.playerIndex = actionMapName == "KeyboardWASD" ? 1 : 2;
            // PlayerInput.playerIndex 是只读的，不能赋值
            // 将 playerScript 的 playerInput 引用设置为刚创建的实例
            try { playerScript.playerInput = newPlayerInput; } catch { }
        }
        newPlayerInput.gameObject.name = $"Player_{device.displayName}_{actionMapName}";

        // 记录设备与玩家的关系
        if (!devicePlayersMap.ContainsKey(device))
            devicePlayersMap[device] = new List<GameObject>();
        devicePlayersMap[device].Add(newPlayerInput.gameObject);
        deviceActiveMaps[device].Add(actionMapName);

        Debug.Log($"已为设备 {device.displayName} 创建玩家，地图 {actionMapName}，当前玩家数 {PlayerInput.all.Count}");

        Debug.Log($"【创建成功】玩家 {newPlayerInput.gameObject.name}，索引 {playerScript?.playerIndex}，地图 {actionMapName}，设备 {device.displayName}");

        // 调试输出当前所有 PlayerInput 的绑定信息，帮助定位为何动作没有触发
        LogAllPlayerInputBindings(actionMapName);

        // 强制同步每个 PlayerInput 的 action map 状态，确保 player1 仅响应 WASD，player2 仅响应 Gamepad 或 Arrows
        try { EnforcePlayerInputMaps(); } catch { }
    }

    // 确保每个 PlayerInput 上启用/禁用正确的 ActionMap，避免不同玩家响应错误的键
    private void EnforcePlayerInputMaps()
    {
        foreach (var pi in PlayerInput.all)
        {
            if (pi == null) continue;
            var p = pi.GetComponent<Player>();
            if (p == null) continue;

            var actions = pi.actions;
            if (actions == null) continue;

            // 禁用所有键盘相关地图，随后启用需要的
            var mapWASD = actions.FindActionMap("KeyboardWASD");
            var mapArrows = actions.FindActionMap("KeyboardArrows");
            var mapGamepad = actions.FindActionMap("Gamepad");

            try { if (mapWASD != null) mapWASD.Disable(); } catch { }
            try { if (mapArrows != null) mapArrows.Disable(); } catch { }
            try { if (mapGamepad != null) mapGamepad.Disable(); } catch { }

            // 根据 player.playerIndex 与 boundDevice 决定启用哪个地图
            if (p.playerIndex == 1)
            {
                try { if (mapWASD != null) { mapWASD.Enable(); foreach (var a in mapWASD.actions) a.Enable(); } } catch { }
            }
            else if (p.playerIndex == 2)
            {
                if (p.boundDevice is Gamepad)
                {
                    try { if (mapGamepad != null) { mapGamepad.Enable(); foreach (var a in mapGamepad.actions) a.Enable(); } } catch { }
                }
                else
                {
                    try { if (mapArrows != null) { mapArrows.Enable(); foreach (var a in mapArrows.actions) a.Enable(); } } catch { }
                }
            }

            // 如果启用了键盘地图，确保 actions.devices 指定为该键盘设备，反之清空
            try
            {
                if (p.boundDevice is Keyboard)
                {
                    pi.actions.devices = new ReadOnlyArray<InputDevice>(new[] { p.boundDevice });
                }
                else if (p.boundDevice is Gamepad)
                {
                    pi.actions.devices = new ReadOnlyArray<InputDevice>(new[] { p.boundDevice });
                }
            }
            catch { }
        }
    }

    private bool IsPlayerIndexTaken(int index)
    {
        foreach (var pi in PlayerInput.all)
        {
            if (pi == null) continue;
            Player player = pi.GetComponent<Player>();
            if (player != null && player.playerIndex == index)
                return true;
        }
        return false;
    }

    private void OnDeviceChanged(InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.Removed)
        {
            // 设备移除时，销毁所有关联的玩家
            if (devicePlayersMap.TryGetValue(device, out var players))
            {
                foreach (var player in players)
                {
                    if (player != null)
                        Destroy(player);
                }
                devicePlayersMap.Remove(device);
            }
            deviceActiveMaps.Remove(device);
        }
        else if (change == InputDeviceChange.Added)
        {
            Debug.Log($"新设备加入: {device.displayName}，等待按键创建玩家...");
        }
    }

    // 供 Player 脚本在 OnDestroy 时调用，清理记录
    public void UnregisterPlayer(GameObject player, InputDevice device, string controlScheme)
    {
        if (devicePlayersMap.TryGetValue(device, out var players))
        {
            players.Remove(player);
            if (players.Count == 0)
                devicePlayersMap.Remove(device);
        }

        if (deviceActiveMaps.TryGetValue(device, out var maps))
        {
            maps.Remove(controlScheme);
            if (maps.Count == 0)
                deviceActiveMaps.Remove(device);
        }
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
}