using UnityEngine;

public class PlayerController2D : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private LayerMask groundLayer;        // 哪些层被视为地面

    [Header("组件引用")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private BoxCollider2D playerCollider;

    private bool isGrounded;

    // 暴露只读属性
    public bool IsGrounded => isGrounded;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (playerCollider == null) playerCollider = GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        // 跳跃输入检测（放在 Update 避免漏帧）
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            Jump();
        }
    }

    private void FixedUpdate()
    {
        // 水平移动
        float moveX = Input.GetAxis("Horizontal");
        rb.velocity = new Vector2(moveX * moveSpeed, rb.velocity.y);

        // 地面检测
        CheckGrounded();
    }

    private void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    }

    private void CheckGrounded()
    {
        // 使用 BoxCast 向下检测
        Bounds bounds = playerCollider.bounds;
        float extraHeight = 0.05f; // 稍微延伸一点
        RaycastHit2D hit = Physics2D.BoxCast(bounds.center, bounds.size, 0f, Vector2.down, extraHeight, groundLayer);
        isGrounded = hit.collider != null;
    }

    // 可视化地面检测
    private void OnDrawGizmosSelected()
    {
        if (playerCollider == null) return;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Bounds bounds = playerCollider.bounds;
        Gizmos.DrawWireCube(bounds.center + Vector3.down * 0.05f, bounds.size);
    }
}