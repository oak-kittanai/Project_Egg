using Fusion;
using Fusion.Addons.Physics;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Windows;

public class CharacterAction : NetworkBehaviour, CharacterInteract
{
    [Header("Ref")]
    CharacterStats stats;
    CharacterAnimation cAnimation;
    Rigidbody2D rb2D;
    Collider2D coll2D;
    [SerializeField] Camera playerCam;

    [Header("Value")]
    [SerializeField] public bool canInteract;
    [SerializeField] public bool pressed;
    [SerializeField] public bool isCarry;

    [Header("Throw")]
    [SerializeField] public LineRenderer _lineRenderer;
    [SerializeField] public Vector2 _worldPos;
    [SerializeField] public bool _isDrawPathAvailable;
    [SerializeField] public Transform _firePoint;
    [SerializeField] public float _step;
    [SerializeField] public float powerMultiplier = 10f;
    public float maxThrowRange = 20f;

    [SerializeField] bool mouse1Press;
    [SerializeField] bool mouse2Hold;

    [SerializeField] float maxPointRadius;

    [Header("Item Interact")]
    [SerializeField] bool _isInteractAble;
    [SerializeField] bool _isAbleSkill;
    [SerializeField] float _interactRadius;
    [SerializeField] bool _busy;

    [Header("Test")]
    public SkillSet characterSkillSet;

    [Header("rock")]
    [SerializeField] public bool carryRock;
    [NetworkPrefab] public NetworkObject rockPrefab;

    private void Awake()
    {
        Setup();
    }

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            playerCam.gameObject.SetActive(true);
        }
        else
        {
            playerCam.gameObject.SetActive(false);
        }
    }

    public override void FixedUpdateNetwork()
    {
        CheckItemInteract();
    }

    public void Setup()
    {
        stats = GetComponent<CharacterStats>();
        playerCam = GetComponentInChildren<Camera>();
        cAnimation = GetComponent<CharacterAnimation>();
    }

    public GameObject CharacterInteract()
    {
        return this.gameObject;
    }

    #region InputZone
    public void UpdateActionInputSkill()
    {
        if (_isAbleSkill)
        {
            if (_busy) return;
        }
    }

    public void MouseInput(bool pressMouse2, bool pressMouse1)
    {
        mouse2Hold = pressMouse2;
        if (pressMouse2)
        {
            _isDrawPathAvailable = true;
        }else _isDrawPathAvailable = false;

        mouse1Press = pressMouse1;
    }

    #region ProjectileCalculateMovement&DrawPath

    private void DrawPath(float v0, float angle, float time, float step)
    {
        step = Mathf.Max(0.01f, step);
        _lineRenderer.positionCount = (int)(time / step) + 2;

        int index = 0;
        for (float t = 0; t <= time; t += step)
        {
            float x = v0 * t * Mathf.Cos(angle);
            float y = Mathf.Abs(v0) * t * Mathf.Sin(angle) + 0.5f * Physics.gravity.y * t * t;

            Vector3 pos = _firePoint.position + new Vector3(x, y, 0);
            _lineRenderer.SetPosition(index, pos);
            index++;
        }

        float finalX = v0 * time * Mathf.Cos(angle);
        float finalY = Mathf.Abs(v0) * time * Mathf.Sin(angle) + 0.5f * Physics.gravity.y * time * time;
        _lineRenderer.SetPosition(index, _firePoint.position + new Vector3(finalX, finalY, 0));
    }

    public void CalculatePath(Vector3 targetPos, out float v0, out float angle, out float time)
    {
        Vector3 dir = Vector3.ClampMagnitude(targetPos - _firePoint.position, 20f);

        float x = Mathf.Max(0.001f, Mathf.Abs(dir.x));
        float y = dir.y;
        float g = -Physics.gravity.y;

        float targetAngle = Mathf.Atan2(y, x);
        angle = Mathf.Max(45f * Mathf.Deg2Rad, targetAngle + (10f * Mathf.Deg2Rad));
        angle = Mathf.Clamp(angle, 0f, 85f * Mathf.Deg2Rad);

        float cos = Mathf.Cos(angle);
        float tan = Mathf.Tan(angle);
        float heightDiff = x * tan - y;

        if (heightDiff <= 0.001f)
        {
            v0 = time = 0;
            return;
        }

        v0 = Mathf.Sqrt(g * x * x / (2f * cos * cos * heightDiff));
        time = x / (v0 * cos);

        if (dir.x < 0) v0 = -v0;
    }
    #endregion

    #region Throw

    public void UpdateCursorPos(Vector2 pos)
    {
        if (playerCam == null) return;

        Vector3 mousePos = playerCam.ScreenToWorldPoint(pos);
        mousePos.z = 0;

        Vector3 directionToMouse = mousePos - _firePoint.position;

        if (directionToMouse.magnitude > maxThrowRange)
        {
            directionToMouse = directionToMouse.normalized * maxThrowRange;
        }

        Vector3 targetPos = _firePoint.position + directionToMouse;

        if (mouse2Hold && carryRock)
        {
            float v0, angle, time;
            CalculatePath(targetPos, out v0, out angle, out time);

            if (time <= 0f)
            {
                _lineRenderer.enabled = false;
                return;
            }

            DrawPath(v0, angle, time, _step);
            _lineRenderer.enabled = true;

            if (mouse1Press)
            {
                mouse2Hold = false;
                _lineRenderer.enabled = false;
                carryRock = false;
                ThrowRock(v0, angle);
            }
        }
        else
        {
            _lineRenderer.enabled = false;
        }
    }

    public void ThrowRock(float v0, float angleRad)
    {
        NetworkObject rockNetObj = Runner.Spawn(rockPrefab, _firePoint.position, Quaternion.identity);

        var nrb = rockNetObj.GetComponent<NetworkRigidbody2D>();
        ThrowAble throwa = rockNetObj.GetBehaviour<ThrowAble>();
        throwa.AlreadyThrow = true;

        if (nrb != null)
        {
            float vx = v0 * Mathf.Cos(angleRad);
            float vy = Mathf.Abs(v0) * Mathf.Sin(angleRad); // Abs(v0) ensures we always throw UP

            Vector2 forceToApply = new Vector2(vx, vy) * nrb.Rigidbody.mass;

            nrb.Rigidbody.AddForce(forceToApply, ForceMode2D.Impulse);
        }
    }

    #endregion

    public void InteractAble(bool press)
    {
        if (_isInteractAble)
        {
            if (_busy) return;

            if (canInteract)
            {
                if (press)
                {
                    pressed = true;
                }
                else
                {
                    pressed = false;
                }
            }
            /*if (input.InteractAction.WasPressedThisFrame())
            {
                Debug.Log("press E");
                Interact(hit);

                if (!_isCarry)
                {
                    if (_canCarry && hit.gameObject.layer == LayerMask.NameToLayer("Player"))
                    {
                        if (_playerToCarry != null)
                        {
                            Debug.Log("Start Carry");

                            CarryCompanion(_playerToCarry);
                            _isCarry = true;
                            stats.StaminaReduce(2);
                        }
                    }
                }
                else
                {
                    _isCarry = false;
                }
            }*/
        }
    }

    #endregion

    #region Interact Zone

    public void CheckItemInteract()
    {
        Vector2 player = transform.position;
        int mask = LayerMask.GetMask("Player", "Interactable", "ThrowAbleProjectile");
        Collider2D[] itemhitInteract = Physics2D.OverlapCircleAll(player, _interactRadius, mask);

        foreach (Collider2D hit in itemhitInteract)
        {
            if (hit == null || hit.gameObject == this.gameObject)
                continue;

            _isInteractAble = true;
            if (hit.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                //Debug.Log("Detect Player");
                canInteract = true;
                Interact(hit);
            }
            else canInteract = false;

            if (hit.gameObject.layer == LayerMask.NameToLayer("Interactable"))
            {
                Debug.Log("Detect Interactable Object");
                canInteract = true;
                Interact(hit);
            }
            else canInteract = false;

            if (hit.gameObject.layer == LayerMask.NameToLayer("ThrowAbleProjectile"))
            {
                canInteract = true; // Work fine
                Interact(hit);
            }
            else canInteract = false;
        }
    }

    public void Interact(Collider2D hit)
    {
        Vector2 player = transform.position;

        if (hit != null)
        {
            _isInteractAble = true;
            Vector2 selfpos = new Vector2(transform.position.x, transform.position.y);
            if (pressed && canInteract)
            {
                switch (hit.gameObject)
                {
                    case GameObject g when g.TryGetComponent<Interactable>(out var obj):
                        Debug.Log("trigger interactable object");
                        if (stats.skinType == characterType.Bird)
                        {
                            cAnimation.SmashAnimation();
                            obj.Interact();
                        }
                        break;

                    case GameObject g when g.TryGetComponent<MoveableObject>(out var moveobj):
                        Debug.Log("trigger moveable object");
                        moveobj.MoveInteract(selfpos);
                        break;
                        
                    case GameObject g when g.TryGetComponent<ThrowAbleItem>(out var item):
                        if (stats.skinType == characterType.Bird)
                        {
                            Debug.Log("Get ThrowItem");
                            carryRock = item.PickupItem();
                        }
                        break;

                    default:
                        //Debug.Log("didn't found any trigger");
                        break;
                }
            }
        }
        else
        {
            _isInteractAble = false;
        }
    }

    /*public void CheckPlayerInteract(Collider2D player)
    {
        switch (player.gameObject)
        {
            case GameObject g when g.TryGetComponent<CharacterInteract>(out var character):
                _playerToCarry = character.CharacterInteract();
                if (_isCarry)
                {
                    character.SetCollider(true);
                }
                else { character.SetCollider(false); }

                _canCarry = true;

                break;

            default:
                _canCarry = false;
                break;
        }
    }*/

    #endregion

    #region CarryOn

    /*public void UpdateCarryPos()
    {
        Vector2 selfPos = new Vector2(transform.position.x, transform.position.y);
        if (stats.MinStamina > 0)
        {
            if (IsDuck)
            {
                _positionToBe = _duckPosition + selfPos;
                _isCarry = true;
            }

            if (IsEagle)
            {
                _positionToBe = _eaglePosition + selfPos;
                _isCarry = true;
            }
        }
        else { Debug.Log("out of stamina"); _isCarry = false; }
    }*/

    private void CarryCompanion(GameObject carryObject)
    {
        if (carryObject != null)
        {
            //UpdateCarryPos();
        }
        else { Debug.Log("can't find carryObject"); };
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 playerPosition = this.gameObject.transform.position;
        Gizmos.DrawWireSphere(playerPosition, _interactRadius);
    }

    public void DropCharacter()
    {
        throw new NotImplementedException(); // add to Drop
    }

    public void SetCollider(bool o)
    {
        coll2D.isTrigger = o;
    }
}



[Serializable]
public class SkillSet
{
    [Header("Eagle")]
    public float _flyDuration;
    public float _flySpeed;


    [Header("Duck")]
    public bool isFloating;
    public float floatingSpeed;
    // floating animation

    public bool isDive;
    public float diveSpeed;
    // dive animation


    // Duck
    public void WaterFloating()
    {
        // Set speed
        int i = 1;
        if (i == 1) // if on top of water
        {
            float speed;
            float walkSpeed = floatingSpeed;
            // floating animation

            speed = walkSpeed;

            if (i == 2) // if press some button to dive
            {
                speed = diveSpeed;
                // dive animation
            }
        }
    }

    

    // Eagle
}
