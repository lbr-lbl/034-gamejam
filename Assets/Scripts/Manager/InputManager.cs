using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager instance;

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
        if (controllerInput == null) {
            Debug.LogError("Controller Input is null! Cannot switch Action Map.");
            return; }
        controllerInput.SwitchCurrentActionMap(actionMapName);
    }
}