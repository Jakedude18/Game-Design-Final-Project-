using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class FeetController : MonoBehaviour
{
    public Transform abdomenObject;
    public Transform rayHeadObject;

    private Vector2 gazePos;
    public float gazeDistance = 2f;

    public int numberOfRays = 10;
    public float rayReachDistance = 4f;
    public float rayAngleRange = 160f;
    public int rayColliderLayerIndex = 6;
    private int raycastLayerMask;
    private List<Vector2> hits;

    private Vector2[] targetPoints;
    private Vector2[] feetPosCurrent;
    private Vector2[] feetPosFrom;
    private Vector2?[] feetPosTo;

    private float[] tValues;
    private AnimationCurve curveEase;
    public float footVelocity = 0.05f;
    public float flailAmount = 2.4f;

    public float retargetThreshold = 2.5f;
    public float feetTargetRadius = 0.1f;
    public float minTargetDistance = 1f;
    public float targetImportance = 4.74f;
    public float targetSwitchIntensity = 4.3f;
    //public bool targetsWithinRange = false;
    //private bool targetUpdateStep = false;

    public int numberOfFeet = 4;
    public float tentacleHeightOffset = 3.6f;
    public GameObject[] tentacleObjects;
    private float[] tentacleRotations;

    private float gizmoAngle;
    private bool drawGizmoAngle;

    private void Start()
    {
        drawGizmoAngle = false;

        numberOfFeet = tentacleObjects.Length;
        targetPoints = new Vector2[numberOfFeet];
        tentacleRotations = new float[numberOfFeet];

        feetPosCurrent = new Vector2[numberOfFeet];
        feetPosFrom = new Vector2[numberOfFeet];
        feetPosTo = new Vector2?[numberOfFeet];

        tValues = new float[numberOfFeet];
        curveEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

        raycastLayerMask = 1 << rayColliderLayerIndex;
        hits = new List<Vector2>();

        for (int i=0; i<numberOfFeet; i++)
        {
            targetPoints[i] = new Vector2(0, 100);
            feetPosCurrent[i] = Vector2.zero;
            feetPosFrom[i] = Vector2.zero;
            tValues[i] = 0f;

            float rand = Random.Range(-50f, 50f);
            Quaternion randRot = Quaternion.Euler(0f, 0f, rand);
            tentacleObjects[i].transform.rotation = randRot;
            tentacleRotations[i] = rand;
        }
    }

    private void Update()
    {
        // check min distance between feet targets
        //float cDist = 999f;
        //targetsWithinRange = false;
        //int tentacleIndex = 0;
        //foreach (Vector2 cTarget in targetPoints)
        //{
        //    cDist = Vector2.Distance(cTarget, tentacleObjects[tentacleIndex].transform.position);
        //    targetsWithinRange = cDist < minTargetDistance;
        //    targetUpdateStep = !targetsWithinRange && !targetUpdateStep;
        //    tentacleIndex++;
        //}

        // update "gaze" position towards the input controller axis
        gazePos.x = abdomenObject.position.x + (Input.GetAxisRaw("Horizontal") * gazeDistance);
        gazePos.y = abdomenObject.position.y + (
            Mathf.Max(0, Input.GetAxisRaw("Vertical") * gazeDistance)
        );

        // raycast from head position
        hits.Clear();
        CastRays();

        // update target points if they are far from body
        RetargetTargets();
    }

    private void FixedUpdate()
    {
        RepositionGameObjects();
    }

    //private void RetargetFeetAirborne()
    //{
    //    Vector2 newPos = (Vector2)abdomenObject.position - new Vector2(0f, 1.5f);
    //    for (int i=0; i<numberOfFeet; i++)
    //    {
    //        if (targetUpdateStep)
    //        {
    //            feetPosFrom[i] = feetPosCurrent[i];
    //            tValues[i] = 0;
    //        }
    //        feetPosTo[i] = OffsetWithNoise(newPos, i, Time.fixedTime);
    //    }
    //    targetUpdateStep = false;
    //    RetargetFeet();
    //}

    private Vector2 OffsetWithNoise(Vector2 pos, int p1, float p2)
    {
        pos.x += (Mathf.PerlinNoise(p1, p2 + p1) - 0.5f) * flailAmount;
        p1 += numberOfFeet;
        pos.y += (Mathf.PerlinNoise(p1, p2 + p1) - 0.5f) * flailAmount;
        return pos;
    }

    private void RepositionGameObjects()
    {
        //for (int i=0; i<numberOfFeet; i++)
        //{
        //    tentacleObjects[i].transform.position = adjustIkBase(i);

        //    TentacleTargetInterface targetComponent = tentacleObjects[i].GetComponent<TentacleTargetInterface>();
        //    targetComponent.targetTransform.position = feetPosCurrent[i];
        //}
        for (int i=0; i<numberOfFeet; i++)
        {
            // calculate hover position;
            // get distance from abdomen to foot target
            float dist = Vector2.Distance(feetPosCurrent[i], abdomenObject.position);

            // find function to clamp/interpolate between current pos
            // and hover pos
            Vector2 newPos = importanceSlerp(feetPosCurrent[i], calculateHoverPos(i), dist);
            //if (i == 5) { Debug.Log(dist); }

            // adjust ikBase transform
            // adjust tentacleTarget transform to function result
            tentacleObjects[i].transform.position = adjustIkBase(i);
            TentacleTargetInterface targetComponent = tentacleObjects[i].GetComponent<TentacleTargetInterface>();
            targetComponent.targetTransform.position = newPos;
        }

        RetargetFeet();
        // check min distance between feet targets
        //float cDist = 999f;
        //targetsWithinRange = false;
        //int tentacleIndex = 0;
        //foreach (Vector2 cTarget in targetPoints)
        //{
        //    cDist = Vector2.Distance(cTarget, tentacleObjects[tentacleIndex].transform.position);
        //    targetsWithinRange = cDist < minTargetDistance;
        //    targetUpdateStep = !targetsWithinRange && !targetUpdateStep;
        //    tentacleIndex++;
        //}
    }

    // calculate targeting importance using a sigmoid interpolation
    private Vector2 importanceSlerp(Vector2 a, Vector2 b, float d)
    {
        float res = -(d - targetImportance);
        res = Mathf.Pow(targetSwitchIntensity, res);
        res = 1 / (1 + res);
        return Vector2.Lerp(a, b, res);
    }

    private Vector2 calculateHoverPos(int index)
    {
        Vector2 res = (Vector2)abdomenObject.position - new Vector2(0f, 1.5f);
        res = OffsetWithNoise(res, index, Time.fixedTime);
        return res;
    }

    public Vector3 adjustIkBase(int i)
    {
        Vector3 res = rayHeadObject.transform.position + new Vector3(0, -0.5f);
        float theta = tentacleRotations[i] * Mathf.Deg2Rad;

        res.x += tentacleHeightOffset * Mathf.Sin(theta);
        res.y += tentacleHeightOffset + (-Mathf.Cos(theta) - 1);

        return res;
    }

    private void RetargetFeet()
    {
        for (int i=0; i<numberOfFeet; i++) {
            if (feetPosTo[i] != null)
            {
                if (Vector2.Distance(feetPosCurrent[i], (Vector2)feetPosTo[i]) > feetTargetRadius) {
                    float smoothT = IncrementTValue(i);
                    //feetPosCurrent[i] = Vector2.Lerp(feetPosFrom[i], (Vector2)feetPosTo[i], smoothT);
                    feetPosCurrent[i] = Vector3.Slerp(feetPosFrom[i], (Vector3)feetPosTo[i], smoothT);

                } else
                {
                    feetPosFrom[i] = (Vector2)feetPosTo[i];
                    feetPosTo[i] = null;
                }
            }
        }
    }

    private float IncrementTValue(int index)
    {
        float res = curveEase.Evaluate(tValues[index]);
        tValues[index] += footVelocity;
        return res;
    }

    private void RetargetTargets()
    {
        for (int i=0; i<targetPoints.Length; i++)
        {
            // retarget points if they are further away than allowed threshold (#1)
            if (hits.Count > 0 && Vector2.Distance(gazePos, targetPoints[i]) > retargetThreshold)
            {
                int chosenHitIndex = Random.Range(0, hits.Count - 1);

                feetPosFrom[i] = targetPoints[i];
                targetPoints[i] = hits[chosenHitIndex];
                feetPosTo[i] = targetPoints[i];
                tValues[i] = 0;
            }
        }
    }

    private void CastRays()
    {
        //float inputAxisAngle = getInputAxisAngle();
        float inputAxisAngle = getVelocityVector();
        DrawAngleGizmo(inputAxisAngle);

        float thetaMin = inputAxisAngle - (rayAngleRange / 2);
        float thetaMax = inputAxisAngle + (rayAngleRange / 2);
        float thetaStep = rayAngleRange / numberOfRays;

        Vector2 rayDirection;

        for (float t=thetaMin; t<=thetaMax; t += thetaStep)
        {
            rayDirection = new Vector2( Mathf.Cos(t), Mathf.Sin(t) );

            RaycastHit2D hit = Physics2D.Raycast
            (
                rayHeadObject.position,
                rayDirection,
                rayReachDistance,
                raycastLayerMask
            );

            if (hit.collider != null)
            {
                hits.Add(hit.point);
            }
        }
    }

    private float getVelocityVector()
    {
        Vector2 vel = abdomenObject.GetComponent<Rigidbody2D>().velocity;
        float res = Mathf.Atan2(vel.y, vel.x);
        return res;
    }

    private float getInputAxisAngle()
    {
        Vector2 gazeDirection = gazePos - (Vector2)rayHeadObject.position;
        float res = Mathf.Atan2(gazeDirection.y, gazeDirection.x);
        return res;
    }

    private void DrawAngleGizmo(float angle)
    {
        gizmoAngle = angle;
        drawGizmoAngle = true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(gazePos, 0.18f);

        //Gizmos.color = Color.yellow;
        //if (hits != null)
        //{
        //    foreach (Vector2 hit in hits)
        //    {
        //        Gizmos.DrawLine(rayHeadObject.position, hit);
        //    }
        //}

        if (drawGizmoAngle)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(rayHeadObject.position, (Vector2)rayHeadObject.position + new Vector2
            (
                Mathf.Cos(gizmoAngle) * rayReachDistance,
                Mathf.Sin(gizmoAngle) * rayReachDistance
            ));
        }

        Gizmos.color = Color.gray;
        if (targetPoints != null)
        {
            foreach (Vector2 targetPoint in targetPoints)
            {
                Gizmos.DrawSphere(targetPoint, 0.18f);
            }
        }

        Gizmos.color = Color.cyan;
        if (feetPosCurrent != null)
        {
            foreach (Vector2? foot in feetPosCurrent)
            {
                if (foot != null)
                {
                    Gizmos.DrawSphere((Vector3)foot, 0.18f);
                    //Gizmos.DrawLine(abdomenObject.position, (Vector3)foot);
                }
            }
        }
    }
}
