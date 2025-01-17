using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class PlayerController : MonoBehaviour
{
    public Rigidbody playerRigidBody { get; private set; }
    private Vector3 downForce = Vector3.down * 10;
    private float waterDensity = 1f;
    private bool isDiving = false;
    private float initialDiveTime = 3f;
    private float initialDiveStartTime;
    private bool isInitialDive = false;
    private int baseStamina = 100;
    private int maxStamina;
    public int currentStamina;
    private int speedLevel;

    public GameObject water;
    public GameObject followerSea;
    public Image staminaImage;
    public SkinnedMeshRenderer playerHead;
    public Renderer headMaterial;
    public float materialColorValue;
    public TextMeshProUGUI mText;
    private void Awake()
    {
        BindEvents();

    }

    private void OnDestroy()
    {
        UnbindEvents();
    }

    private void BindEvents()
    {
        EventManager.onDive += DiveDown;
        EventManager.onOceanColorSelected += OnOceanColorChanged;
    }

    private void UnbindEvents()
    {
        EventManager.onDive -= DiveDown;
        EventManager.onOceanColorSelected -= OnOceanColorChanged;
    }

    void Start()
    {
        playerRigidBody = GetComponent<Rigidbody>();
        downForce = downForce / waterDensity;
    }

    private void Update()
    {
        if (currentStamina <= 254)
        {
            playerHead.materials[1].color = new Color(1, materialColorValue, materialColorValue);            
        }
        else if (currentStamina > 255)
        {
            playerHead.materials[1].color = new Color(1, 1, 1);
        }
        mText.text = ((int)transform.position.y + "m").ToString();
    }


    void FixedUpdate()
    {
        if (Input.GetMouseButton(0))
        {
            if (playerRigidBody == null)
            {
                return;
            }
            if (GameManager.Instance.GameState == eGameState.Gameplay)
            {
                Dive();
                followerSea.SetActive(true);

            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            
            if (GameManager.Instance.GameState == eGameState.Gameplay)
            {
                Stop();
            }
        }
        if (playerRigidBody == null)
        {
            return;
        }

        if (isDiving)
        {
            float currentTime = Time.time;
            float t;
            t = (currentTime-initialDiveStartTime)/initialDiveTime;
            if (isInitialDive)
            {
                DiveLerp(t);
            }
        }

    }
    
    void InitPowerUpLevels()
    {
        var powerUpsManager = GameManager.Instance.GetPowerUpsManager();
        maxStamina = baseStamina + 10 * powerUpsManager.StaminaLevel;
        currentStamina = maxStamina;
        speedLevel = powerUpsManager.SpeedLevel;
    }

    void Dive()
    {

        if(water != null)
        {
            Destroy(water, 0.5f);
        }

        playerRigidBody.velocity = downForce;
        LerpMoveHorizontally();
    }

    void LerpMoveHorizontally()
    {
        var xPosition = transform.position.x + Input.GetAxis("Mouse X") * 0.1f;
        xPosition = Mathf.Clamp(xPosition, 248.5f, 251.5f);
        transform.position = new Vector3(xPosition, transform.position.y, transform.position.z);
    }
    void Stop()
    {
        playerRigidBody.velocity = Vector3.zero;
        EventManager.onPlayerStop?.Invoke();

    }
    void DiveDown()
    {
        InitPowerUpLevels();
        InitDownForce();
        initialDiveStartTime = Time.time;
        isInitialDive = true;
        isDiving = true;
        StartCoroutine(DepleteRecoverStamina());
    }

    void InitDownForce()
    {
        var force = downForce + Vector3.down * speedLevel;
        downForce = force;
    }

    void DiveLerp(float t)
    {
        playerRigidBody.velocity = Vector3.Lerp(downForce, Vector3.zero, t);
        if (t >= 1)
        {
            isInitialDive = false;
        }
    }

    void OnGameOver()
    {
        Stop();
    }

    private void OnOceanColorChanged(OceanColor oceanColor)
    {
        waterDensity = oceanColor.density;
    }

    IEnumerator DepleteRecoverStamina()
    {
        WaitForSeconds waitTime = new WaitForSeconds(1);
        while (true)
        {
            if (playerRigidBody.velocity.y == 0)
            {
                currentStamina+= 5;
                staminaImage.DOColor(new Color(0, 0.4509804f, 1, 0), 0.25f);
                materialColorValue += 0.01f;
            }
            else
            {
                currentStamina -= 10;
                staminaImage.DOColor(new Color(0, 0.4509804f, 1, 1), currentStamina * 0.0001f);
                materialColorValue -= 0.02f;
            }
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
            float ratio = 1f - currentStamina / (float) maxStamina;
            EventManager.onStaminaUpdated?.Invoke(ratio);
            if (currentStamina == 0)
            {
                EventManager.OnGameOver();
                OnGameOver();
                yield break;
            }
            yield return waitTime;
        }
        
    }

}

public enum eDirection
{
    up,
    down
}
