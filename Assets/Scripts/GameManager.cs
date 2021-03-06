﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    /* Manages Overtaking, Health, Death, Camera, "Respawn", and level generation
     * If Overtaking occurs, will set roles and positions of players
     * Will update player's health periodically
     * Death is implemented in the HealthUpdate function
     */

    public static Healthbars Player1Health; //temporary
    public static Healthbars Player2Health; //temporary
    public static ImageMoving Player1Icon;
    public static ImageMoving Player2Icon;

    public float OvertakeTime;
    private float _time_of_overtake = 0;

    //health stuff
    public float HealthTickDelay;
    public int HealthTickDamage;
    public int MaxHealth;
    public int OffScreenDamage;
    private float _time_since_healthdrain = 0;

    private Camera _camera;
    private LevelManager _level_manager;
    private GameObject[] _players;
    private GameObject[] _playersLeft;
    private GameObject _djIcon1;
    private GameObject _gIcon1;
    private GameObject _rIcon1;
    private GameObject _djIcon2;
    private GameObject _gIcon2;
    private GameObject _rIcon2;
    private List<PlayerComponents> _player_components;

    private GameObject _leader;
    private int _leader_num;
    private float _camera_height;
    private float _leader_height;
    private bool _overtaking; //is overtaking occuring

    //level stuff
    public float LevelBuffer = 0; //height player needs to jump to get to next level
    private float _total_level_height = 0;
    private List<Vector3> _respawn_points = new List<Vector3>();


    void Start()
    {
        _camera = FindObjectOfType<Camera>();
        _camera_height = _camera.transform.position.y;
        _level_manager = FindObjectOfType<LevelManager>();
        _players = GameObject.FindGameObjectsWithTag("Player");
        _djIcon1 = GameObject.Find("DoubleJump1");
        _djIcon2 = GameObject.Find("DoubleJump2");
        _gIcon1 = GameObject.Find("Gun1");
        _gIcon2 = GameObject.Find("Gun2");
        _rIcon1 = GameObject.Find("Reload1");
        _rIcon2 = GameObject.Find("Reload2");

        _player_components = new List<PlayerComponents>();
        _leader = GameObject.Find("Fake Leader");
        _leader_num = 0;
        Player1Health = GameObject.Find("P1Health").GetComponent<Healthbars>();
        Player2Health = GameObject.Find("P2Health").GetComponent<Healthbars>();
        Player1Icon = GameObject.Find("ObiWanHealthBarPic").GetComponent<ImageMoving>();
        Player2Icon = GameObject.Find("AnakinHealthBarPic").GetComponent<ImageMoving>();

        //the point of player_components is we don't want to be calling GetComponent every Update cycle
        //apparently it can make the game laggy, so we want to "stash" those components
        int i = -1;
        foreach (GameObject player in _players)
        {
            _player_components.Add(new PlayerComponents(player, GetPlayerNumber(player), MaxHealth));
            player.transform.position = new Vector3(i, 4, 0);
            i++;
        }
        _level_manager.UpdateCurrentLevel(0);
        _respawn_points.AddRange(_level_manager.GetRespawnPoints());

        _djIcon1.SetActive(true);
        _djIcon2.SetActive(true);
        _gIcon1.SetActive(false);
        _gIcon2.SetActive(false);
        _rIcon1.SetActive(false);
        _rIcon2.SetActive(false);
    }

    private void Update()
    {
        //Overtaking management
        foreach (GameObject player in _players)
        {
            if (_overtaking)
                break;
            if (player != null)
            {
                //if overtaking occurs
                if (player.transform.position.y > _leader.transform.position.y && player.name != _leader.name)
                {
					player.GetComponent<sound>().playSound("overtake");
                    _leader = player;
                    _leader_num = GetPlayerNumber(player);
                    _overtaking = true;
                    Overtake(player.transform.position);
                    break;
                }
            }
        }

        //Camera Management
        _leader_height = _leader.transform.position.y;
        _camera_height = _camera.transform.position.y;
        if (_leader_height > _camera_height + _camera.orthographicSize * 0.5f) //leader height should be <= 3/4 of camera height
            _camera.transform.position = new Vector3(0, _leader_height - _camera.orthographicSize * 0.5f, -10f);
        if (_leader_height < _camera_height) //leader height should be >= 1/2 of camera height
            _camera.transform.position = new Vector3(0, _leader_height, -10f);

        //Level Management
        if (_leader_height > _level_manager.GetCurrentLevelHeight() + _total_level_height - _camera.orthographicSize / 1.5)
        {
            _total_level_height += _level_manager.GetCurrentLevelHeight() + LevelBuffer;
            _level_manager.UpdateCurrentLevel(_total_level_height);
            //add new respawn points
            foreach (Vector3 respawnpoint in _level_manager.GetRespawnPoints())
                _respawn_points.Add(new Vector3(respawnpoint.x, respawnpoint.y + _total_level_height, 0));
        }

        //Respawn Management (Chasers falling off screen) (want this to be below camera management)
        foreach (PlayerComponents player in _player_components)
        {
            if (player.Reference != null)
            {
                if (player.Reference.transform.position.y < _camera_height - _camera.orthographicSize)
                {
                    player.Rb.velocity = new Vector3(0, 0, 0);
                    player.SetHealth(player.Health - OffScreenDamage);
                    player.Reference.transform.position = FindClosetRespawnPoint(_leader_height - _camera.orthographicSize / 2f);
                }
            }
        }

        //Health Management
        if (Time.time - _time_since_healthdrain >= HealthTickDelay && !_overtaking)
        {
            HealthUpdate();
            _time_since_healthdrain = Time.time;
        }
        foreach (PlayerComponents player in _player_components)
        {
            if (player.Reference != null)
            {
                if (player.Number == 1)
                {
                    Player1Health.SetValue(player.Health);
                    Player1Icon.NewYValue(Player1Health.GetValue());
                }
                else if (player.Number == 2)
                {
                    Player2Health.SetValue(player.Health);
                    Player2Icon.NewYValue(Player2Health.GetValue());
                }
            }
        }

    }

    private void Overtake(Vector3 leaderPosition) //leaderPosition = position of new leader
    {
        Vector3 oldLeaderPosition = leaderPosition;
        Vector3 oldCameraPosition = _camera.transform.position;
        Vector3 newLeaderPosition = FindClosetRespawnPoint(leaderPosition.y + _camera.orthographicSize / 2f);
        Vector3 newCameraPosition = new Vector3(0, newLeaderPosition.y - _camera.orthographicSize / 3, -10);

        _time_of_overtake = Time.time;
        _leader.GetComponent<ChaserMovement>().enabled = false;
        _leader.GetComponent<Rigidbody2D>().velocity = new Vector3(0, 0, 0);
        _leader.GetComponent<Rigidbody2D>().simulated = false;
        _leader.GetComponent<Animator>().SetBool("Overtaking", true);
        StartCoroutine(Lerp(oldLeaderPosition, newLeaderPosition, oldCameraPosition, newCameraPosition));
        StartCoroutine(ForceLeader()); //force circle animation
        ForceStun(); //stun chasers
    }

    private IEnumerator Lerp(Vector3 oldLeaderPosition, Vector3 newLeaderPosition, Vector3 oldCameraPosition, Vector3 newCameraPosition)
    {
        float time = 0;
        while (time <= OvertakeTime)
        {
            time = Mathf.Abs(Time.time - _time_of_overtake);
            _leader.transform.position = Vector3.Lerp(oldLeaderPosition, newLeaderPosition, time / OvertakeTime);
            _camera.transform.position = Vector3.Lerp(oldCameraPosition, newCameraPosition, time / OvertakeTime);
            yield return null;
        }

        _leader.transform.position = newLeaderPosition;
        _camera.transform.position = newCameraPosition;
        SetRoles();
        _leader.GetComponent<Rigidbody2D>().simulated = true;
        _leader.GetComponent<Animator>().SetBool("Overtaking", false);
        _overtaking = false;
    }

    private IEnumerator ForceLeader()
    {
        GameObject forcefield = _leader.GetComponent<ChaserMovement>().Forcefield;
        forcefield.SetActive(true);
        forcefield.transform.localScale = new Vector3(0.25f, 0.25f, 1);
        float time = 0;
        while (time <= OvertakeTime)
        {
            forcefield.transform.localScale = new Vector3(0.25f, 0.25f, 1) + forcefield.transform.localScale;
            time = Mathf.Abs(Time.time - _time_of_overtake);
            yield return null;
        }
        forcefield.SetActive(false);
    }

    private void ForceStun() //Stun Chasers during overtake
    {
        foreach (PlayerComponents player in _player_components)
        {
            if (player.Number != _leader_num)
            {
                player.Chaser.enabled = true;
                player.Chaser.Stun(OvertakeTime);
                player.Leader.enabled = false;
                player.Gun.enabled = false;
            }
        }
    }

    private void SetRoles()
    {
        foreach (PlayerComponents player in _player_components)
        {
            if (player.Number != _leader_num) //set Chasers
            {
                player.Leader.FinishLine.SetActive(false);
                player.Leader.enabled = false;
                player.Gun.enabled = false;
                player.Chaser.enabled = true;
            }
            else //set Leader
            {
                player.Chaser.enabled = false;
                player.Leader.enabled = true;
                player.Leader.FinishLine.SetActive(true);
                player.Gun.enabled = true;
                player.Invincible = false;
            }
        }
        if (_leader_num == 1)
        {
            _djIcon1.SetActive(false);
            _djIcon2.SetActive(true);
            _gIcon1.SetActive(true);
            _gIcon2.SetActive(false);
            _rIcon1.SetActive(true);
            _rIcon2.SetActive(false);
        }
        else if (_leader_num == 2)
        {
            _djIcon1.SetActive(true);
            _djIcon2.SetActive(false);
            _gIcon1.SetActive(false);
            _gIcon2.SetActive(true);
            _rIcon1.SetActive(false);
            _rIcon2.SetActive(true);
        }
    }

    private void HealthUpdate()
    {
        if (_leader_num != 0)
            foreach (PlayerComponents player in _player_components)
            {
                if (player.Number != _leader_num && player.Health > 0)
                    player.SetHealth(player.Health - HealthTickDamage);
                if (player.Health <= 0)
                {
                    Destroy(player.Reference.gameObject);
                    EndGame(player.Number);
                }
            }
        //Update Health Canvas (temporary)
    }

    private void EndGame(int num)
    {
        if (num == 2) //Player 1 Won
        {
            SceneManager.LoadScene(2);
        }
        else if (num == 1) //Player 2 Won
        {
            SceneManager.LoadScene(3);
        }
    }

    //Helper function that finds closest respawn point to a certain location
    private Vector3 FindClosetRespawnPoint(float maxHeight)
    {
        for (int i = _respawn_points.Count - 1; i >= 0; i--)
            if (_respawn_points[i].y < maxHeight)
                return _respawn_points[i];
        Debug.LogError("No Respawn Points?");
        return new Vector3();
    }

    private int GetPlayerNumber(GameObject player)
    {
        switch (player.name)
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
                return 0;
        }
    }
}

public class PlayerComponents
{
    public GameObject Reference; //reference to original player object
    public Rigidbody2D Rb;
    public int Number;
    public int Health;
    public ChaserMovement Chaser;
    public LeaderMovement Leader;
    public BasicGun Gun;
    public bool Invincible = false;

    public PlayerComponents(GameObject r, int n, int h)
    {
        Reference = r;
        Rb = r.GetComponent<Rigidbody2D>();
        Number = n;
        Health = h;
        Chaser = r.GetComponent<ChaserMovement>();
        Leader = r.GetComponent<LeaderMovement>();
        Gun = r.GetComponent<BasicGun>();
    }

    public void SetHealth(int h)
    {
        if (!Invincible)
        Health = h;
    }
}
 