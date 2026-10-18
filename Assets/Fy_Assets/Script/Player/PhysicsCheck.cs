using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ºÏ≤‚Ω≈±æ
// ºÏ≤‚Ω≈±æ
public class PhysicsCheck : MonoBehaviour
{
    [Header("µÿ√ÊºÏ≤‚")]
    public Transform groundCheckPoint;
    public float groundCheckRadius;
    public LayerMask groundLayer;
    public bool isTouchGround;
    public LayerMask flyGroundLayer;
    public bool isTouchFlyGround;

    [Header("«Ω±⁄ºÏ≤‚")]
    public Transform wallCheckPoint;
    public float wallCheckLength;
    public LayerMask wallLayer;
    public bool isTouchWall;
    private void Update()
    {
        Check();
    }

    public void Check()
    {
        // ºÏ≤‚ «∑ÒΩ”¥•µÿ√Ê
        isTouchGround = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);
        isTouchFlyGround = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, flyGroundLayer);

        // ºÏ≤‚ «∑ÒΩ”¥•«Ω±⁄
        isTouchWall = Physics2D.Raycast(wallCheckPoint.position, transform.right * transform.localScale.x, wallCheckLength, wallLayer);
        // ≈ ≈¿ºÏ≤‚
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        // ‘⁄ Scene  ”Õº÷–ªÊ÷∆ºÏ≤‚∑∂Œß
        Gizmos.DrawWireSphere((Vector2)groundCheckPoint.position, groundCheckRadius);
        Gizmos.DrawLine(wallCheckPoint.position, wallCheckPoint.position + Vector3.right * transform.localScale.x * wallCheckLength);
    }
}


