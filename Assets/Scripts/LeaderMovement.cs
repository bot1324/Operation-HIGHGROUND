﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class LeaderMovement : MonoBehaviour
{
    /*
     * Movement script used by "Leader" players
     * 
     */
    public float Move_Speed;
    public float Jump_Force;
    private int _player;
    private Rigidbody2D _rigidbody;
    private Transform _transform;
    private Vector3 _facingRight = new Vector3(0, 0, 0);
    private Vector3 _facingLeft = new Vector3(0, 180, 0);
    private Animator _animator;

    private bool _grounded = false;
    private bool _touchingWallLeft = false;
    private bool _touchingWallRight = false;
    private bool _normalJump = false;
    private bool _wallJump = false;
    private int _wallJumpCounter = -1;

    //temporary solution
    public GameObject FinishLine;

	private sound _sound;

    void Start()
    {
        _player = PlayerController();
        _rigidbody = GetComponent<Rigidbody2D>();
        _transform = transform;
        _animator = GetComponent<Animator>();
		_sound = GetComponent<sound>();
    }

    private void Update()
    {
        Vector2 position = new Vector2(transform.position.x, transform.position.y - 0.35f);
        _grounded = Physics2D.BoxCast(position, new Vector2(1.2f, 0.001f), 0, Vector2.down, 1.00f, 1) ? true : false;
        _animator.SetBool("Grounded", _grounded);
        _touchingWallLeft = Physics2D.BoxCast(position, new Vector2(0.001f, 1.7f), 0, Vector2.left, 0.9f, 1) ? true : false;
        _touchingWallRight = Physics2D.BoxCast(position, new Vector2(0.001f, 1.7f), 0, Vector2.right, 0.9f, 1) ? true : false;
        _animator.SetBool("TouchingWall", _touchingWallLeft || _touchingWallRight);

        if (_grounded)
        {
            _normalJump = true;
            _wallJump = true;
            _animator.SetFloat("Walljumps", 1);
        }
        if (_touchingWallLeft)
        {
            if (_wallJumpCounter == 1 || _wallJumpCounter == -1)
            {
                _wallJump = true;
                _animator.SetFloat("Walljumps", 1);
            }
            _wallJumpCounter = 0;
        }
        if (_touchingWallRight)
        {
            if (_wallJumpCounter == 0 || _wallJumpCounter == -1)
            {
                _wallJump = true;
                _animator.SetFloat("Walljumps", 1);
            }
            _wallJumpCounter = 1;
        }

        if (Input.GetButtonDown("JumpA_p" + _player) || Input.GetButtonDown("JumpB_p" + _player))
                Jump();

        //red/blue bar
        FinishLine.transform.position = new Vector2 (0f, transform.position.y);

        if (SceneManager.GetActiveScene().name == "Tutorial" && Input.GetButtonDown("StartButton" + _player))
            SceneManager.LoadScene(0);
    }

    private void FixedUpdate()
    {
        float movement = Input.GetAxis("Horizontal_p" + _player);
        if (Mathf.Abs(movement) > 0.9f)
            Move(movement);
        else
            StopMoving();
    }

    private void Move(float direction)
    {
        if (direction > 0)
        {
            _rigidbody.velocity = new Vector2(Move_Speed * Time.fixedDeltaTime, _rigidbody.velocity.y);
            _transform.eulerAngles = _facingRight;
        }
        else
        {
            _rigidbody.velocity = new Vector2(-Move_Speed * Time.fixedDeltaTime, _rigidbody.velocity.y);
            _transform.eulerAngles = _facingLeft;
        }
        _animator.SetBool("Moving", true);
    }

    private void StopMoving()
    {
        _rigidbody.velocity = new Vector2(0, _rigidbody.velocity.y);
        _animator.SetBool("Moving", false);
    }

    private void Jump()
    {
		//Play sound if able to jump
		//if (_normalJump || ((_touchingWallLeft || _touchingWallRight) && _wallJump))
			

        if (_normalJump)
        {
			_sound.playSound("jump");
			_rigidbody.velocity = new Vector2(_rigidbody.velocity.x, Jump_Force);
            _normalJump = false;
            return;
        }
        if (_touchingWallLeft && _wallJump)
        {
			_sound.playSound("wallJump");
			_rigidbody.velocity = new Vector2(_rigidbody.velocity.x, Jump_Force);
            _wallJump = false;
            _animator.SetFloat("Walljumps", 0);
            return;
        }
        if (_touchingWallRight && _wallJump)
        {
			_sound.playSound("wallJump");
			_rigidbody.velocity = new Vector2(_rigidbody.velocity.x, Jump_Force);
            _wallJump = false;
            _animator.SetFloat("Walljumps", 0);
            return;
        }
    }

    private int PlayerController()
    {
        string name = gameObject.name;
        switch (name)
        {
            case "Player1":
                return 1;
            case "Player2":
                return 2;
            case "Player3":
                return 3;
            case "Player4":
                return 4;
            default:
                Debug.LogError("Player object must be name Playerx, with x being the number of the player");
                return 0;
        }
    }
}
