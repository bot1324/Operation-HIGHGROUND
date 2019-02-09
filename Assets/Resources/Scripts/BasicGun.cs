﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicGun : MonoBehaviour
{
    /*
     * Basic gun that can be attached to a player
     * Will allow a player to shoot the attached bullet
     * Sets rotation of the bullet to direction player is shooting
     */
    public float ReloadTime = 1;
    public GameObject BulletPrefab;
    private int _player;
    private Transform _holder;
    private Camera _cam;
    private float _time_last_shot = -100; //initialized to -100 so that there isn't an initial reload time

    void Start() {
        _player = PlayerController();
        _holder = transform;
        _cam = FindObjectOfType<Camera>();
        BulletPrefab = (GameObject)Resources.Load("Prefabs/BasicBullet", typeof(GameObject));
    }

    void Update()
    {
        if (Input.GetAxis("Fire_p" + _player) > 0 || Input.GetButton("Fire_p" + _player))
        {
            if (Time.time - _time_last_shot > ReloadTime)
            {
                Shoot();
                _time_last_shot = Time.time;
            }      
        }
    }

    public void Shoot()
    {
        //Controls for mouse
        //Vector3 direction = Input.mousePosition - _cam.WorldToScreenPoint(_holder.position);
        //float angle = Mathf.Atan2(direction.y, direction.x) * 360 / (2 * Mathf.PI);

        //Controls for controller
        float xaxis = Input.GetAxisRaw("AimX_p" + _player);
        float yaxis = Input.GetAxisRaw("AimY_p" + _player);
        float angle = Mathf.Atan2(yaxis, xaxis) * 360 / (2 * Mathf.PI);

        Quaternion rotation = Quaternion.Euler(0, 0, angle);
        GameObject bullet = Instantiate(BulletPrefab, _holder.position, rotation, _holder);
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