using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

public class InputManager : MonoBehaviour
{
    public static InputManager instance;

    private PlayerInputManager playerInputManager;

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
        // 查找场景中的 PlayerInputManager 并订阅 onPlayerJoined 回调
        playerInputManager = FindObjectOfType<PlayerInputManager>();
        if (playerInputManager != null)
        {
            playerInputManager.onPlayerJoined += HandlePlayerJoined;
        }
        else
        {
            Debug.LogWarning("场景中未找到 PlayerInputManager，无法自动分配 Action Map。");
        }
    }

    private void OnDisable()
    {
        if (playerInputManager != null)
            playerInputManager.onPlayerJoined -= HandlePlayerJoined;
    }

    /// <summary>
    /// 当 PlayerInputManager 有新玩家加入时调用，自动根据 playerIndex 分配 Action Map。
    /// 0 -> "Player1", 1 -> "Player2"，超出范围则回退到 "Player2"。
    /// </summary>
    /// <param name="playerInput">加入的 PlayerInput 实例</param>
    private void HandlePlayerJoined(PlayerInput playerInput)
    {
        if (playerInput == null) return;

        // 根据索引决定 Action Map 名称（可根据需要调整规则）
        string actionMapName = playerInput.playerIndex == 0 ? "Player1" : "Player2";

        // 尝试通过 Player 组件来分配（保持现有 AssignActionMapToPlayer 使用路径）
        Player playerComponent = playerInput.GetComponent<Player>();
        if (playerComponent != null)
        {
            AssignActionMapToPlayer(playerComponent, actionMapName);
        }
        else
        {
            // 回退：直接在 PlayerInput 上切换 Action Map
            playerInput.SwitchCurrentActionMap(actionMapName);
            Debug.Log($"玩家加入（无 Player 组件），已在 PlayerInput 上切换到 Action Map: {actionMapName}");
        }
    }

    /// <summary>
    /// 为玩家角色设置指定的 Action Map
    /// </summary>
    public void AssignActionMapToPlayer(Player player, string actionMapName)
    {
        PlayerInput pi = player.GetComponent<PlayerInput>();
        if (pi == null)
        {
            Debug.LogError($"玩家 {player.name} 没有 PlayerInput 组件！");
            return;
        }

        pi.SwitchCurrentActionMap(actionMapName);
        Debug.Log($"玩家 {player.name} 已切换到 Action Map: {actionMapName}");
    }

    /// <summary>
    /// 为基地建造控制器设置指定的 Action Map
    /// </summary>
    public void AssignActionMapToController(PlayerInput controllerInput, string actionMapName)
    {
        if (controllerInput == null) return;
        controllerInput.SwitchCurrentActionMap(actionMapName);
    }
}