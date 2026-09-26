using System.Collections;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance;

    [Header("Tracking Settings")]
    public Transform target;
    public Vector3 offset = new Vector3(0f, 14f, -8f);
    [Range(0.01f, 0.3f)] public float smoothTime = 0.08f;

    [HideInInspector]
    public Vector3 shakeOffset = Vector3.zero;

    private Vector3 currentVelocity = Vector3.zero;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = target.position + offset;
        Vector3 smoothedPos = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, smoothTime);

        transform.position = smoothedPos + shakeOffset;
    }

    public void TriggerShake(float duration, float magnitude)
    {
        StopAllCoroutines();
        StartCoroutine(DoShake(duration, magnitude));
    }

    IEnumerator DoShake(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            shakeOffset = new Vector3(
                Random.Range(-1f, 1f) * magnitude,
                Random.Range(-0.5f, 0.5f) * magnitude,
                Random.Range(-1f, 1f) * magnitude
            );
            elapsed += Time.deltaTime;
            yield return null;
        }
        shakeOffset = Vector3.zero;
    }
}