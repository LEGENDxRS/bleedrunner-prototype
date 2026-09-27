using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    [Header("Shooting Config")]
    public Transform firePoint;
    public float fireRate = 0.15f;
    public float range = 30f;
    public LayerMask hitLayers;

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

    void Awake()
    {
        baseFireRate = fireRate;

        if (tracer != null)
        {
            tracer.useWorldSpace = true;
            tracer.positionCount = 2;
        }
    }

    void Update()
    {
        if (TimeManager.Instance != null && (TimeManager.Instance.isDead || TimeManager.Instance.isPaused)) return;

        bool isFiring = Mouse.current != null && Mouse.current.leftButton.isPressed;

        if (isFiring && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    void Shoot()
    {
        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Vector3 shootDir = firePoint != null ? firePoint.forward : transform.forward;
        Vector3 endPoint = origin + shootDir * range;

        if (hasPiercingSlugs)
        {
            RaycastHit[] hits = Physics.RaycastAll(origin, shootDir, range, hitLayers);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                EnemyTarget target = hits[i].collider.GetComponent<EnemyTarget>();
                if (target != null)
                {
                    target.TakeDamage(1);
                }
            }
        }
        else
        {
            if (Physics.Raycast(origin, shootDir, out RaycastHit hit, range, hitLayers))
            {
                endPoint = hit.point;
                EnemyTarget target = hit.collider.GetComponent<EnemyTarget>();
                if (target != null)
                {
                    target.TakeDamage(1);
                }
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