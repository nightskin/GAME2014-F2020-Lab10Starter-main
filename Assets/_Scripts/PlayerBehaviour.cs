using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cinemachine;

public class PlayerBehaviour : MonoBehaviour
{
    [Header("Controls")]
    public Joystick joystick;
    public float joystickHorizontalSensitivity;
    public float joystickVerticalSensitivity;
    public float horizontalForce;
    public float verticalForce;

    [Header("FX")]
    // dust trail
    private ParticleSystem dustTrail;
    // sounds
    enum SFX
    {
        HOP,
        HIT,
        DIE
    }
    public AudioSource[] sounds;
    // camera shaking
    public CinemachineVirtualCamera vcam;
    public CinemachineBasicMultiChannelPerlin perlin;
    public float shake_intensity;
    public float max_shake_time;
    public float shakeTimer;
    public bool shaking = false;

    [Header("Platform Detection")]
    public bool isGrounded;
    public bool isJumping;
    public bool isCrouching;
    public Transform spawnPoint;
    public Transform lookAheadPoint;
    public Transform lookInFrontPoint;
    public LayerMask collisionGroundLayer;
    public LayerMask collisionWallLayer;
    public RampDirection rampDirection;
    public bool onRamp;
   
    [Header("Player Abilities")] 
    public int health;
    public int lives;
    public BarController healthBar;
    public Animator livesHUD;

    private Rigidbody2D m_rigidBody2D;
    private SpriteRenderer m_spriteRenderer;
    private Animator m_animator;
    private RaycastHit2D groundHit;

    // Start is called before the first frame update
    void Start()
    {
        health = 100;
        lives = 3;
        dustTrail = GetComponentInChildren<ParticleSystem>();
        m_rigidBody2D = GetComponent<Rigidbody2D>();
        m_spriteRenderer = GetComponent<SpriteRenderer>();
        m_animator = GetComponent<Animator>();
        sounds = GetComponents<AudioSource>();

        vcam = FindObjectOfType<CinemachineVirtualCamera>();
        perlin = vcam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        _LookInFront();
        _LookAhead();
        _Move();

        if(shaking)
        {
            shakeTimer -= Time.deltaTime;
            if(shakeTimer <= 0)
            {
                perlin.m_AmplitudeGain = 0.0f;
                shakeTimer = max_shake_time;
                shaking = false;
            }
        }

    }

    private void _LookInFront()
    {
        if (!isGrounded)
        {
            rampDirection = RampDirection.NONE;
            return;
        }

        var wallHit = Physics2D.Linecast(transform.position, lookInFrontPoint.position, collisionWallLayer);
        if (wallHit && isOnSlope())
        {
            rampDirection = RampDirection.UP;
        }
        else if (!wallHit && isOnSlope())
        {
            rampDirection = RampDirection.DOWN;
        }

        Debug.DrawLine(transform.position, lookInFrontPoint.position, Color.red);
    }

    private void _LookAhead()
    {
        groundHit = Physics2D.Linecast(transform.position, lookAheadPoint.position, collisionGroundLayer);

        isGrounded = (groundHit) ? true : false;

        Debug.DrawLine(transform.position, lookAheadPoint.position, Color.green);
    }

    private bool isOnSlope()
    {
        if (!isGrounded)
        {
            onRamp = false;
            return false;
        }

        if (groundHit.normal != Vector2.up)
        {
            onRamp = true;
            return true;
        }

        onRamp = false;
        return false;
    }

    void _Move()
    {
        if (isGrounded)
        {
            if (!isJumping && !isCrouching)
            {
                if (joystick.Horizontal > joystickHorizontalSensitivity)
                {
                    // move right
                    m_rigidBody2D.AddForce(Vector2.right * horizontalForce * Time.deltaTime);
                    transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                    if (onRamp && rampDirection == RampDirection.UP)
                    {
                        m_rigidBody2D.AddForce(Vector2.up * horizontalForce * 0.5f * Time.deltaTime);
                    }
                    else if (onRamp && rampDirection == RampDirection.DOWN)
                    {
                        m_rigidBody2D.AddForce(Vector2.down * horizontalForce * 0.5f * Time.deltaTime);
                    }
                    CreateDustTrail();

                    m_animator.SetInteger("AnimState", (int)PlayerAnimationType.RUN);
                }
                else if (joystick.Horizontal < -joystickHorizontalSensitivity)
                {
                    // move left
                    m_rigidBody2D.AddForce(Vector2.left * horizontalForce * Time.deltaTime);
                    transform.localScale = new Vector3(-1.0f, 1.0f, 1.0f);
                    if (onRamp && rampDirection == RampDirection.UP)
                    {
                        m_rigidBody2D.AddForce(Vector2.up * horizontalForce * 0.5f * Time.deltaTime);
                    }
                    else if (onRamp && rampDirection == RampDirection.DOWN)
                    {
                        m_rigidBody2D.AddForce(Vector2.down * horizontalForce * 0.5f * Time.deltaTime);
                    }
                    CreateDustTrail();

                    m_animator.SetInteger("AnimState", (int)PlayerAnimationType.RUN);
                }
                else
                {
                    m_animator.SetInteger("AnimState", (int)PlayerAnimationType.IDLE);
                }
            }

            if ((joystick.Vertical > joystickVerticalSensitivity) && (!isJumping))
            {
                // jump
                m_rigidBody2D.AddForce(Vector2.up * verticalForce);
                m_animator.SetInteger("AnimState", (int) PlayerAnimationType.JUMP);
                isJumping = true;
                sounds[(int)SFX.HOP].Play();

                CreateDustTrail();
            }
            else
            {
                isJumping = false;
            }

            if ((joystick.Vertical < -joystickVerticalSensitivity) && (!isCrouching))
            {
                m_animator.SetInteger("AnimState", (int)PlayerAnimationType.CROUCH);
                isCrouching = true;
            }
            else
            {
                isCrouching = false;
            }
        }

    }

    private void CreateDustTrail()
    {
        //dustTrail.GetComponent<Renderer>().material.SetColor("_Color", trailColor);
        dustTrail.Play();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // respawn
        if (other.gameObject.CompareTag("DeathPlane"))
        {
            LoseLife();
        }

        if (other.gameObject.CompareTag("Bullet"))
        {
            TakeDamage(10);
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Enemy"))
        {
            TakeDamage(15);
        }
    }

    private void ShakeCamera()
    {
        perlin.m_AmplitudeGain = shake_intensity;
        shaking = true;
    }

    public void LoseLife()
    {
        lives -= 1;
        livesHUD.SetInteger("LivesState", lives);
        sounds[(int)SFX.DIE].Play();
        if (lives > 0)
        {
            health = 100;
            healthBar.SetValue(health);
            transform.position = spawnPoint.position;
        }
        else
        {
            SceneManager.LoadScene("End");
        }
        
    }

    public void TakeDamage(int damage)
    {
        ShakeCamera();
        sounds[(int)SFX.HIT].Play();
        health -= damage;
        healthBar.SetValue(health);

        if (health <= 0)
        {
            LoseLife();
        }
    }
}
