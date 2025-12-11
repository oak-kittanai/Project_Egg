using Fusion;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Windows;

public class CharacterAction : NetworkBehaviour, CharacterInteract
{
    [Header("Ref")]
    InputControl controller;
    CharacterStats stats;
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

    public bool fly;

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
        controller = GetComponent<InputControl>();
        stats = GetComponent<CharacterStats>();
        playerCam = GetComponentInChildren<Camera>();
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

            /*if (input.SkillPressAction.WasPressedThisFrame())
            {
                _busy = true;
                // do skill

                // return _busy = false; return _busy to false
            }*/
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
    private void DrawPath(Vector3 dir, float v0, float angle, float time, float step)
    {
        float maxTime = time; // draw full arc
        step = Mathf.Max(0.01f, step);

        _lineRenderer.positionCount = (int)(maxTime / step) + 1;

        int index = 0;
        for (float t = 0; t <= maxTime; t += step)
        {
            float x = v0 * t * Mathf.Cos(angle);
            float y = v0 * t * Mathf.Sin(angle) + 0.5f * Physics.gravity.y * t * t;

            Vector3 pos = _firePoint.position + dir * x + new Vector3(0, y, 0);
            _lineRenderer.SetPosition(index, pos);
            index++;
        }
    }

    /*private float QuadraticEquation(float a, float b, float c, float sign)
    {
        return (-b + sign * Mathf.Sqrt(b * b - 4 * a * c)) / (2 * a);
    }*/

    public void CalculatePath(Vector3 targetPos, float _unusedHeight, out float v0, out float angle, out float time)
    {
        float xt = targetPos.x;
        float yt = targetPos.y;
        float g = -Physics.gravity.y;

        // Choose a constant angle (your arc shape)
        angle = 45f * Mathf.Deg2Rad; // you can change this if you want

        float cosA = Mathf.Cos(angle);
        float sinA = Mathf.Sin(angle);

        float denom = 2f * cosA * cosA * (xt * Mathf.Tan(angle) - yt);

        if (denom <= 0f)
        {
            // no valid solution -> fallback
            v0 = 0;
            time = 0;
            return;
        }

        v0 = Mathf.Sqrt(g * xt * xt / denom);

        // total flight time
        time = xt / (v0 * cosA);
    }
    #endregion

    #region Throw

    public void UpdateCursorPos(Vector2 pos)
    {
        if (playerCam == null)
        {
            Debug.Log("can't find playerCam");
            return;
        }

        Vector3 mousePos = playerCam.ScreenToWorldPoint(pos);
        _worldPos = mousePos - _firePoint.position;

        if (mouse2Hold && carryRock)
        {
            Vector3 targetPos = new Vector3(_worldPos.x, _worldPos.y, 0);
            Vector3 dir = targetPos.normalized;

            float v0, angle, time;
            CalculatePath(targetPos, 0f, out v0, out angle, out time);

            if (time <= 0f)
            {
                _lineRenderer.enabled = false;
                return;
            }

            DrawPath(dir, v0, angle, time, _step);
            _lineRenderer.enabled = _isDrawPathAvailable;

            if (mouse1Press)
            {
                StopAllCoroutines();
                StartCoroutine(ThrowRockCoroutine(dir, v0, angle, time));
            }
        }
        else
        {
            _lineRenderer.enabled = false;
        }
    }

    // Throw
    IEnumerator Coroutine_Movement(Vector3 direction, float v0, float angle, float totalTime)
    {
        Vector3 startPos = transform.position;
        Vector3 horizontalDir = new Vector3(direction.x, 0, direction.z).normalized;

        float t = 0f;
        while (t < totalTime)
        {
            float x = v0 * t * Mathf.Cos(angle);
            float y = v0 * t * Mathf.Sin(angle) + 0.5f * Physics.gravity.y * t * t;

            transform.position = startPos + horizontalDir * x + Vector3.up * y;

            t += Time.deltaTime;
            yield return null;
        }

        // Final snap to ground (use the same direction at totalTime)
        float xfinal = v0 * totalTime * Mathf.Cos(angle);
        transform.position = startPos + horizontalDir * xfinal + Vector3.up * 0f;
    }

    public IEnumerator ThrowRockCoroutine(Vector3 direction, float v0, float angleRad, float totalTime)
    {
        NetworkObject rockNetObj = Runner.Spawn(rockPrefab, _firePoint.position, Quaternion.identity);
        Transform rock = rockNetObj.transform;

        Vector3 startPos = _firePoint.position;
        float t = 0f;

        while (t < totalTime)
        {
            float x = v0 * t * Mathf.Cos(angleRad);
            float y = v0 * t * Mathf.Sin(angleRad) + 0.5f * Physics.gravity.y * t * t;

            rock.position = startPos + new Vector3(direction.x * x, y, 0);

            t += Time.deltaTime;
            yield return null;
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
        int mask = LayerMask.GetMask("Player", "Interactable", "ThrowAble");
        Collider2D[] itemhitInteract = Physics2D.OverlapCircleAll(player, _interactRadius, mask);

        foreach (Collider2D hit in itemhitInteract)
        {
            if (hit == null || hit.gameObject == this.gameObject)
                continue;

            _isInteractAble = true;
            if (hit.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                Debug.Log("Detect Player");
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

            if (hit.gameObject.layer == LayerMask.NameToLayer("ThrowAble"))
            {
                Debug.Log("Detect Throwable item");
                canInteract = true;
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
                        obj.Interact();
                        break;

                    case GameObject g when g.TryGetComponent<MoveableObject>(out var moveobj):
                        Debug.Log("trigger moveable object");
                        moveobj.MoveInteract(selfpos);
                        break;

                    case GameObject g when g.TryGetComponent<ThrowAbleItem>(out var item):
                        Debug.Log("Get ThrowItem");
                        item.PickupItem();
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
