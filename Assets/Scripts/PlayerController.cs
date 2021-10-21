using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public bool isPC;
    public abstract class Command
    {
        public abstract void Execute();
    }

    public class JumpFunction : Command
    {
        public override void Execute()
        {
            Jump();
        }
    }

    public class TelekinesisFunction : Command
    {
        public override void Execute()
        {
            Telekinesis();
        }
    }


    public static void Telekinesis()
    {
        
    }

    public static void Jump()
    {
        
    }

    public static void DoMove()
    {
        Command keySpace = new JumpFunction();
        Command keyX = new TelekinesisFunction();
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            keySpace.Execute();
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            keyX.Execute();
        }
    }
    
    
    public CharacterController characterController;
    public float speed = 3;
    

    public Animator animator;
    
    // camera and rotation
    public Transform cameraHolder;
    public float mouseSensitivity = 2f;
    public float upLimit = -50;
    public float downLimit = 50;
    
    // gravity
    private float gravity = 9.87f;
    private float verticalSpeed = 0;

    public Joystick Joystick;

    public RectTransform playerNode;
    Vector3 worldStart;
    
    void Update()
    {
        if(isPC)
        {
            Move();
            Rotate();
        }
    }

    private void Awake()
    {
        worldStart = Vector3.zero;
        if (isPC)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
            Joystick.JoystickMoveHandle += OnJoystickMove;
    }

    public void Rotate()
    {
        float horizontalRotation = Input.GetAxis("Mouse X");
        float verticalRotation = Input.GetAxis("Mouse Y");
        
        transform.Rotate(0, horizontalRotation * mouseSensitivity, 0);
        cameraHolder.Rotate(-verticalRotation*mouseSensitivity,0,0);

        Vector3 currentRotation = cameraHolder.localEulerAngles;
        if (currentRotation.x > 180) currentRotation.x -= 360;
        currentRotation.x = Mathf.Clamp(currentRotation.x, upLimit, downLimit);
        cameraHolder.localRotation = Quaternion.Euler(currentRotation);
    }

    private void Move()
    {
        float horizontalMove = Input.GetAxis("Horizontal");
        float verticalMove = Input.GetAxis("Vertical");

        if (characterController.isGrounded) verticalSpeed = 0;
        else verticalSpeed -= gravity * Time.deltaTime;
        Vector3 gravityMove = new Vector3(0, verticalSpeed, 0);
        
        Vector3 move = transform.forward * verticalMove + transform.right * horizontalMove;
        characterController.Move(speed * Time.deltaTime * move + gravityMove * Time.deltaTime);
        
        animator.SetBool("isWalking", verticalMove != 0 || horizontalMove != 0);
        UpdatePlayerNodePos();
    }

    public float RotateSpeed = 15f;

    private void OnJoystickMove(Vector2 deltaPos)
    {
        float moveValue = deltaPos.y / 125;
        float rotValue = deltaPos.x / 125;

        if (characterController.isGrounded) verticalSpeed = 0;
        else verticalSpeed -= gravity * Time.deltaTime;
        Vector3 gravityMove = new Vector3(0, verticalSpeed, 0);

        Vector3 move = transform.forward * moveValue;
        characterController.Move(speed * Time.deltaTime * move + gravityMove * Time.deltaTime);

        transform.Rotate(Vector3.up, rotValue * Time.deltaTime * RotateSpeed);

        animator.SetBool("isWalking", moveValue != 0 || rotValue != 0);
        UpdatePlayerNodePos();
    }

    void UpdatePlayerNodePos()
    {
        Vector3 delta = transform.position - worldStart;
        if (playerNode)
        {
            playerNode.anchoredPosition = new Vector2(delta.x / 4000f, delta.z / 4000f) * 300;
            playerNode.localEulerAngles = new Vector3(0, 0, 180 - transform.localEulerAngles.y);
        }
    }  
}
