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
    public GameObject playerPrefab;          // ���Ԥ���壨����� PlayerInput �� Player �ű���
    public int maxPlayers = 2;                // ��������

    private PlayerControl playerControl;      // ���ɵ������װ��
    private Dictionary<InputDevice, HashSet<string>> deviceActiveMaps = new(); // �豸 -> �Ѽ���ĵ�ͼ������
    private Dictionary<InputDevice, List<GameObject>> devicePlayersMap = new(); // �豸 -> ���豸���Ƶ�����б�

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        // ��ɵ�����ʼ��
        instance = this;
        DontDestroyOnLoad(gameObject.transform.root.gameObject);
        playerControl = new PlayerControl();
    }

    private void Update()
    {
        // �ֶ���ѯ���벢·�ɵ� player1/player2 �� externalMoveInput������ InputAction ӳ���ͻ
        try
        {
            var players = PlayerInput.all.Select(pi => pi == null ? null : pi.GetComponent<Player>()).Where(p => p != null).ToList();
            var p1 = players.FirstOrDefault(p => p.playerIndex == 1);
            var p2 = players.FirstOrDefault(p => p.playerIndex == 2);

            // ���˲��ң�������δ��ȷ���ã����Ը��� controlScheme ����豸�ƶ�
            if (p1 == null)
            {
                p1 = players.FirstOrDefault(p => p.controlScheme == "KeyboardWASD" || (p.boundDevice is Keyboard && p.controlScheme == "KeyboardWASD"));
            }
            if (p2 == null)
            {
                p2 = players.FirstOrDefault(p => p.controlScheme == "KeyboardArrows" || p.controlScheme == "Gamepad" || (p.boundDevice is Gamepad) || (p.boundDevice is Keyboard && p.controlScheme == "KeyboardArrows"));
            }

            // �����ˣ������δ�ҵ� p1/p2��������˳����䣨��һ��Ϊ p1���ڶ���Ϊ p2��
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
                // ͬʱ��ȡ�ֱ����ͷ���룺��ͷ���ȣ������£�������ʹ���ֱ�
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
                        // ��� boundDevice ���� Gamepad�������ֱ����ӣ�Ҳ��ȡ��һ���ֱ���Ϊ��ѡ
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

                // ��ͷ���ȸ����ֱ�
                var finalV = arrowsV != Vector2.zero ? arrowsV : gpV;
                p2.externalMoveInput = finalV;
                if (finalV != Vector2.zero)
                    Debug.Log($"[InputManager] p2.externalMoveInput = {finalV} (arrows={arrowsV}, gamepad={gpV})");

                // Ϊ KeyboardArrows �ṩ��Ծ֧�֣���Ϊ����Ƶ���Լ������ֶ�·�ɣ���
                // ����⵽�ϼ�ͷ����һ֡������������ڵ���ʱ��ֱ�������䴹ֱ�ٶ�ʵ����Ծ��
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
        // ����Ĭ����ң�player1 ʹ�� WASD
        if (Keyboard.current != null)
        {
            TryCreatePlayer(Keyboard.current, "KeyboardWASD");
        }

        // player2 ����ʹ�õ�һ���ֱ�������ʹ�÷����
        if (maxPlayers >= 2)
        {
            if (Gamepad.all.Count > 0)
            {
                TryCreatePlayer(Gamepad.all[0], "Gamepad");
            }
            else if (Keyboard.current != null)
            {
                // ��ֱ�Ӵ��� KeyboardArrows �� PlayerInput���Ա����� WASD ֱ�ӳ�ͻ��
                // ��Ϊ�ӳٵ���Ұ��¼�ͷ��ʱ������ͨ�� OnArrowsJoin ��������
                // TryCreatePlayer(Keyboard.current, "KeyboardArrows");
            }
        }
    }

    private void OnEnable()
    {
        playerControl.Enable();

        // ע������¼�
        playerControl.Gamepad.Set.performed += OnGamepadJoin;          // �ֱ� Y ��ť
        playerControl.KeyboardWASD.Move.performed += OnWASDJoin;      // WASD �����
        playerControl.KeyboardArrows.Move.performed += OnArrowsJoin;  // ����������

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
            // ��������ֱ�������� player2�����ͷ����Ӧ���� player2���ֱ����ȣ�
            bool gamepadAssignedToPlayer2 = PlayerInput.all.Any(pi =>
            {
                if (pi == null) return false;
                var p = pi.GetComponent<Player>();
                return p != null && p.playerIndex == 2 && p.boundDevice is Gamepad;
            });

            if (gamepadAssignedToPlayer2)
            {
                Debug.Log("���Լ�ͷ���������ֱ����� player2���ֱ����ȣ�");
                return;
            }

            // ���� player2����ͷ������ֻ����δ�� player2 �������
            if (!IsPlayerIndexTaken(2))
            {
                TryCreatePlayer(keyboard, "KeyboardArrows");
            }
            else
            {
                // ��� player2 �Ѵ��ڣ��������ֱ������ģ�������Ҫ�Ѽ�ͷ����·�ɸ��� player2 �� externalMoveInput
                var player2 = PlayerInput.all.Select(pi => pi.GetComponent<Player>()).FirstOrDefault(p => p != null && p.playerIndex == 2);
                if (player2 != null)
                {
                    // ��ȡ��ǰ��ͷ����д�� externalMoveInput
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
        // ������Ѵ�����
        if (PlayerInput.all.Count >= maxPlayers)
        {
            Debug.Log($"�Ѵ��������� ({maxPlayers})���޷����������");
            return;
        }

        // �����豸�Ƿ��Ѽ���õ�ͼ����ֹ�ظ�������
        if (deviceActiveMaps.TryGetValue(device, out var maps))
        {
            if (maps.Contains(actionMapName))
            {
                Debug.Log($"�豸 {device.displayName} �Ѽ����ͼ {actionMapName}������");
                return;
            }
        }
        else
        {
            deviceActiveMaps[device] = new HashSet<string>();
        }

        // �����������Ƿ�ռ�ã����1 = WASD�����2 = �����/�ֱ���
        if (actionMapName == "KeyboardWASD" && IsPlayerIndexTaken(1))
        {
            Debug.Log("���1 (WASD) �Ѵ��ڣ��޷�����");
            return;
        }
        if ((actionMapName == "KeyboardArrows" || actionMapName == "Gamepad") && IsPlayerIndexTaken(2))
        {
            Debug.Log("���2 (�����/�ֱ�) �Ѵ��ڣ��޷�����");
            return;
        }

        // ������ͬһ���������ϴ��������ң�WASD �� ��ͷ ʹ�ò�ͬ�İ󶨣�������Ϊÿ����ҿ�¡ actions �Ա��⹲��״̬��ͻ

        // �ֱ��豸���Ƚ����������ҵİ󶨣�ȷ���豸��ռ
        if (device is Gamepad)
        {
            foreach (var pi in PlayerInput.all.Where(p => p != null))
            {
                if (pi.user.valid)
                    pi.user.UnpairDevice(device);
            }
        }

        // ʵ������ң�������豸
        PlayerInput newPlayerInput = PlayerInput.Instantiate(playerPrefab, pairWithDevice: device);
        if (newPlayerInput == null)
        {
            Debug.LogError("PlayerInput.Instantiate ʧ�ܣ�");
            return;
        }

        // �������� Player �ű��Ļ�����Ϣ������ Player.Start ��δ��������ǰ���е��� index Ϊ 0
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

        // ��¡ actions asset ȷ�� WASD/Arrows ��ͬһ�����ϲ��ụ��Ӱ��
        try
        {
            if (newPlayerInput.actions != null)
            {
                var cloned = UnityEngine.Object.Instantiate(newPlayerInput.actions);
                newPlayerInput.actions = cloned;
                // ���� KeyboardArrows��ͬһ�����ϵĵڶ���ң�����Ҫ�� PlayerInput ֱ�Ӽ������̣�
                // ���ǽ��� InputManager.Update ���ֶ���ȡ��ͷ������д�� player2 �� moveInput�������� WASD ��ͻ
                try
                {
                    // ���� PlayerInput �� actions �޶�Ϊ��Ե��豸��ȷ�� action map �ܽ��ո��豸�¼�
                    try { newPlayerInput.actions.devices = new ReadOnlyArray<InputDevice>(new[] { device }); } catch { }
                }
                catch { }
            }
        }
        catch { }

        // �ֶ������Ӧ�� Action Map
        if (!string.IsNullOrEmpty(actionMapName) && newPlayerInput.actions != null)
        {
            var map = newPlayerInput.actions.FindActionMap(actionMapName);
            if (map != null)
            {
                // �Ƚ������е�ͼ������ʽ����Ŀ���ͼ��ȷ������������
                try
                {
                    foreach (var m in newPlayerInput.actions.actionMaps)
                        m.Disable();
                }
                catch { }

                map.Enable();
                // ȷ�� map �ڵ����ж���Ҳ������
                try
                {
                    foreach (var a in map.actions)
                        a.Enable();
                }
                catch { }
                // ����Ǽ���ӳ�䣬ȷ����ͬһ keyboard ������ PlayerInput �Ͻ����෴�ļ���ӳ�䣬���⻥����ռ
                try
                {
                    if (device is Keyboard)
                    {
                        string otherMapName = null;
                        if (actionMapName == "KeyboardWASD") otherMapName = "KeyboardArrows";
                        else if (actionMapName == "KeyboardArrows") otherMapName = "KeyboardWASD";

                        if (!string.IsNullOrEmpty(otherMapName))
                        {
                            // ���´����� PlayerInput �Ͻ�����һ�����̵�ͼ��������ڣ�
                            try
                            {
                                var other = newPlayerInput.actions.FindActionMap(otherMapName);
                                if (other != null) other.Disable();
                            }
                            catch { }

                            // �����е� PlayerInput ʵ���ϣ����ñ���Ҫ���õĵ�ͼ��actionMapName����
                            // ȷ��������Ҳ�����Ӧ����ҵİ���ӳ��
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
                // �л� PlayerInput ��ǰ��ͼ�������� PlayerInput.currentActionMap ������ȷֵ��
                try { newPlayerInput.SwitchCurrentActionMap(actionMapName); } catch { }

                // ���Ⳣ�Խ��豸������ҵ� InputUser ���
                try
                {
                    var user = newPlayerInput.user;
                    if (user.valid)
                    {
                        UnityEngine.InputSystem.Users.InputUser.PerformPairingWithDevice(device, user);
                    }
                }
                catch { }

                // ���� actions.devices ����ȫ���޸ģ���Ӱ������ asset������ȷ������ҵ� action map �� actions ����
                try { newPlayerInput.actions.Enable(); } catch { }

                // ��� actionMapName �� KeyboardArrows �� Gamepad���� playerScript.playerIndex ��Ϊ 2 ȷ�� PlayerMoveState ��ȷʶ��
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

                Debug.Log($"��� {newPlayerInput.gameObject.name} �л�����ͼ {actionMapName}");
            }
            else
            {
                Debug.LogError($"δ�ҵ���ͼ {actionMapName}��������Ч���");
                Destroy(newPlayerInput.gameObject);
                return;
            }
        }

        // ������ҽű��е���Ϣ
        Player playerScript = newPlayerInput.GetComponent<Player>();
        if (playerScript != null)
        {
            Debug.Log(111);
            playerScript.boundDevice = device;
            playerScript.controlScheme = actionMapName;
            playerScript.playerIndex = actionMapName == "KeyboardWASD" ? 1 : 2;
            // PlayerInput.playerIndex ��ֻ���ģ����ܸ�ֵ
            // �� playerScript �� playerInput ��������Ϊ�մ�����ʵ��
            try { playerScript.playerInput = newPlayerInput; } catch { }
        }
        newPlayerInput.gameObject.name = $"Player_{device.displayName}_{actionMapName}";

        // ��¼�豸����ҵĹ�ϵ
        if (!devicePlayersMap.ContainsKey(device))
            devicePlayersMap[device] = new List<GameObject>();
        devicePlayersMap[device].Add(newPlayerInput.gameObject);
        deviceActiveMaps[device].Add(actionMapName);

        Debug.Log($"��Ϊ�豸 {device.displayName} ������ң���ͼ {actionMapName}����ǰ����� {PlayerInput.all.Count}");

        Debug.Log($"�������ɹ������ {newPlayerInput.gameObject.name}������ {playerScript?.playerIndex}����ͼ {actionMapName}���豸 {device.displayName}");

        // ���������ǰ���� PlayerInput �İ���Ϣ��������λΪ�ζ���û�д���
        LogAllPlayerInputBindings(actionMapName);

        // ǿ��ͬ��ÿ�� PlayerInput �� action map ״̬��ȷ�� player1 ����Ӧ WASD��player2 ����Ӧ Gamepad �� Arrows
        try { EnforcePlayerInputMaps(); } catch { }
    }

    // ȷ��ÿ�� PlayerInput ������/������ȷ�� ActionMap�����ⲻͬ�����Ӧ����ļ�
    private void EnforcePlayerInputMaps()
    {
        foreach (var pi in PlayerInput.all)
        {
            if (pi == null) continue;
            var p = pi.GetComponent<Player>();
            if (p == null) continue;

            var actions = pi.actions;
            if (actions == null) continue;

            // �������м�����ص�ͼ�����������Ҫ��
            var mapWASD = actions.FindActionMap("KeyboardWASD");
            var mapArrows = actions.FindActionMap("KeyboardArrows");
            var mapGamepad = actions.FindActionMap("Gamepad");

            try { if (mapWASD != null) mapWASD.Disable(); } catch { }
            try { if (mapArrows != null) mapArrows.Disable(); } catch { }
            try { if (mapGamepad != null) mapGamepad.Disable(); } catch { }

            // ���� player.playerIndex �� boundDevice ���������ĸ���ͼ
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

            // ��������˼��̵�ͼ��ȷ�� actions.devices ָ��Ϊ�ü����豸����֮���
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
            // �豸�Ƴ�ʱ���������й��������
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
            Debug.Log($"���豸����: {device.displayName}���ȴ������������...");
        }
    }

    // �� Player �ű��� OnDestroy ʱ���ã�������¼
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

    // ���ԣ���ӡ��ǰ���� PlayerInput �İ��豸��Ϣ
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