using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform abdomen;
    public Transform bounds;

    public float trackCoefficient = 3.5f;
    public float clampCoefficient = 0.8f;
    public float snapCoefficient = 6f;

    private AnimationCurve trackEase;
    private Func<float, float> sigmoid;

    private Vector2 wallBoundsMin;
    private Vector2 wallBoundsMax;

    private Vector3 newPos;

    private void Start()
    {
        InitTrackSpeed();
        sigmoid = x => 1 / ((float)Math.Exp(-clampCoefficient * x));

        InitBounds();
    }

    private void InitTrackSpeed()
    {
        float v = trackCoefficient / 10f;
        trackEase = AnimationCurve.EaseInOut(-10f, -v, 10f, v);
    }

    public void InitBounds()
    {
        Camera camera = Camera.main;
        Vector2 halfCamSize = new Vector2(
            camera.orthographicSize * camera.aspect,
            camera.orthographicSize
        );

        wallBoundsMin = (Vector2)bounds.position + halfCamSize - ((Vector2)bounds.localScale / 2);
        wallBoundsMax = (Vector2)bounds.position - halfCamSize + ((Vector2)bounds.localScale / 2);

        bounds.GetComponent<SpriteRenderer>().enabled = false;
    }

    private void Update()
    {
        Vector2 vec = abdomen.position - transform.position;
        float newX = transform.position.x + trackEase.Evaluate(vec.x);
        float newY = transform.position.y + trackEase.Evaluate(vec.y);
        
        // if there was a way to make this a "soft clamp" i'd do it
        newX = Mathf.Clamp(newX, wallBoundsMin.x, wallBoundsMax.x);
        newY = Mathf.Clamp(newY, wallBoundsMin.y, wallBoundsMax.y);

        newPos = new Vector3(newX, newY, -10f);
    }

    private void FixedUpdate() { transform.position = newPos; }

    public IEnumerator CameraSnap()
    {
        trackCoefficient *= snapCoefficient;
        InitTrackSpeed();
        yield return new WaitForSeconds(2f);
        trackCoefficient /= snapCoefficient;
        InitTrackSpeed();
    }

    // none of these functions are working but i dont know why
    // trying to implement something like this: https://www.desmos.com/calculator/4arrtq1rty
    private float SoftClampSig(float t, float min, float max)
    {
        float range = max - min;

        if (t < min) { return min - (range * sigmoid((t - min) / range)); }
        else if (t > max) { return max + (range * sigmoid((max - t) / range)); }
        else { return t; }
    }

    private float SoftClampLog(float t, float min, float max, float k)
    {
        float range = max - min;

        if (t < min) { return min - (range * k * Mathf.Log(1 - ((t - min) / range * (1 - Mathf.Exp(-1 / k))))); }
        else if (t > max) { return max + (range * k * Mathf.Log(1 - ((max - t) / range * (1 - Mathf.Exp(-1 / k))))); }
        else { return t; }
    }

    private float SoftClampPow(float t, float min, float max, float k)
    {
        float range = max - min;

        if (t < min) { return min; }
        if (t > max) { return max; }

        float falloff = Mathf.Pow((t - min) / range, k);
        return min + range * falloff;
    }

    private float SoftClampTrig(float t, float min, float max, float k)
    {
        t = Mathf.Clamp(t, min, max);

        float mid = (min + max) / 2;
        float range = max - min;
        //return mid + ((range / Mathf.PI) * Mathf.Asin((2 * (t - mid)) / range));
        return mid + ((range / 2) * Mathf.Sin((MathF.PI * (t - mid)) / range));
    }
}
