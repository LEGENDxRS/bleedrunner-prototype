using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    [Header("Shooting Config")]
    public Transform firePoint;
    public float fireRate = 0.15f;
    public float range = 35f;
    public float projectileRadius = 0.2f;
    public LayerMask hitLayers = ~0;

    [Header("Perk Upgrades")]
    public bool hasPiercingSlugs = false;

    [Header("Tracer Visuals")]
    public LineRenderer tracer;
    public bool singleFrameTracer = true;
    [Range(0.01f, 0.2f)] public float customTracerDuration = 0.03f;

    [Header("Recoil Camera Shake")]
    public bool enableRecoilShake = true;
    [Range(0.01f, 0.2f)] public float recoilDuration = 0.04f;
    [Range(0.01f, 0.5f)] public float recoilMagnitude = 0.15f;

    private float nextFireTime;
    private Coroutine tracerRoutine;
    private float baseFireRate;
    private Collider playerCol;

    void Awake()
    {
        baseFireRate = fireRate;
        playerCol = GetComponentInParent<Collider>();

        if (tracer != null)
        {
            tracer.useWorldSpace = true;
            tracer.positionCount = 2;
        }

        if (hitLayers.value == 0) hitLayers = ~0;
    }

    void Update()
    {
        if (TimeManager.Instance != null && (TimeManager.Instance.isDead || TimeManager.Instance.isPaused)) return;

        // PC Mouse Fire OR Mobile Joystick Fire
        bool pcFiring = Mouse.current != null && Mouse.current.leftButton.isPressed;
        bool mobileFiring = MobileControls.Instance != null && MobileControls.Instance.isFiring;

        if ((pcFiring || mobileFiring) && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    void Shoot()
    {
        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        origin.y = 0.6f;

        Vector3 shootDir = transform.forward;
        if (firePoint != null) shootDir = firePoint.forward;
        shootDir.y = 0f;
        shootDir.Normalize();

        Vector3 endPoint = origin + (shootDir * range);

        RaycastHit[] hits = Physics.SphereCastAll(origin, projectileRadius, shootDir, range, hitLayers);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == playerCol || hit.collider.CompareTag("Player") || hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            EnemyTarget enemy = hit.collider.GetComponent<EnemyTarget>();
            if (enemy != null)
            {
                enemy.TakeDamage(1);
                if (!hasPiercingSlugs)
                {
                    endPoint = hit.point;
                    break;
                }
            }
            else
            {
                endPoint = hit.point;
                break;
            }
        }

        if (tracer != null)
        {
            if (tracerRoutine != null) StopCoroutine(tracerRoutine);
            tracerRoutine = StartCoroutine(RenderTracer(origin, endPoint));
        }

        if (enableRecoilShake && CameraFollow.Instance != null)
        {
            CameraFollow.Instance.TriggerShake(recoilDuration, recoilMagnitude);
        }
    }

    IEnumerator RenderTracer(Vector3 start, Vector3 end)
    {
        tracer.positionCount = 2;
        tracer.SetPosition(0, start);
        tracer.SetPosition(1, end);
        tracer.enabled = true;

        if (singleFrameTracer) yield return null;
        else yield return new WaitForSeconds(customTracerDuration);

        tracer.enabled = false;
        tracerRoutine = null;
    }

    public void ResetShootingStats()
    {
        fireRate = baseFireRate;
        hasPiercingSlugs = false;
    }
}