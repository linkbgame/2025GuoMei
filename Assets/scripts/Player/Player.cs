using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Player : MonoBehaviour
{
    private Rigidbody2D rb;

    public float speed;
    private float inputX;
    private float inputY;
    private Vector2 movementInput;

    private Vector3 offset;

    private void Awake()
    {
        offset = Camera.main.transform.position - transform.position;
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        PlayerInput();
        Camera.main.transform.position = transform.position + offset;
    }

    private void FixedUpdate()
    {
        Movement();
    }

    private void PlayerInput()
    {
        // if (inputY == 0)
        inputX = Input.GetAxisRaw("Horizontal");
        // if (inputX == 0)
        inputY = Input.GetAxisRaw("Vertical");

        if (inputX != 0 && inputY != 0)
        {
            inputX = inputX * 0.6f;
            inputY = inputY * 0.6f;
        }

        //走路状态速度
        if (Input.GetKey(KeyCode.LeftShift))
        {
            inputX = inputX * 0.5f;
            inputY = inputY * 0.5f;
        }
        movementInput = new Vector2(inputX, inputY);

        movementInput = new Vector2(inputX, inputY);
    }

    private void Movement()
    {
        rb.MovePosition(rb.position + movementInput * speed * Time.deltaTime);
    }
}
