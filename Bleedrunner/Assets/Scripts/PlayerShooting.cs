using System.Collections;
using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Shooting Config")]
    public Transform firePoint;
    public float fireRate = 0.15f;
    public float range = 30f;
    public LayerMask hitLayers;

    [Header("Visuals")]
    public LineRenderer tracer;
    public float tracerDuration = 0.04f;

    private float nextFireTime;

    void Update()
    {
        if (TimeManager.Instance != null && TimeManager.Instance.isDead) return;

        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    void Shoot()
    {
        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Vector3 shootDir = transform.forward;
        Vector3 endPoint = origin + shootDir * range;

        if (Physics.Raycast(origin, shootDir, out RaycastHit hit, range, hitLayers))
        {
            endPoint = hit.point;

            // Damage enemy if hit
            EnemyTarget target = hit.collider.GetComponent<EnemyTarget>();
            if (target != null)
            {
                target.TakeDamage(1);
            }
        }

        // 1. Draw bullet tracer
        if (tracer != null)
        {
            StartCoroutine(RenderTracer(origin, endPoint));
        }

        // 2. Firing recoil shake
        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.TriggerShake(0.06f, 0.25f);
        }
    }

    IEnumerator RenderTracer(Vector3 start, Vector3 end)
    {
        tracer.enabled = true;
        tracer.SetPosition(0, start);
        tracer.SetPosition(1, end);

        yield return new WaitForSeconds(tracerDuration);

        tracer.enabled = false;
    }
}