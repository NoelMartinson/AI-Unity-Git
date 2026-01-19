using UnityEngine;

public class AutonomousAgent : AIAgent
{
    [SerializeField] Movement movement;
    [SerializeField] Perception seekPerception;
    [SerializeField] Perception fleePerception;

    [Header("Wander")]
    [SerializeField] float wanderRadius = 1;
    [SerializeField] float wanderDistance = 1;
    [SerializeField] float wanderDisplacement = 1;

    float wanderAngle = 0.0f;

    void Start()
    {
        // random within circle degrees (random range 0.0f-360.0f) 
        wanderAngle = Random.Range(0.0f, 360.0f);
    }

    void Update()
    {
        bool hasTarget = false;

        if (seekPerception != null)
        {
            // get perceived game objects 
            var gameObjects = seekPerception.GetGameObjects();
            if (gameObjects.Length > 0)
            {
                hasTarget = true;
                // move towards (seek) nearest game object 
                Vector3 force = Seek(gameObjects[0]);
                movement.ApplyForce(force);
            }
        }

        if (movement == null) return;

        // Optional: if you only want one behavior at a time, pick one.
        // If you want them to combine, keep as-is (forces add up).

        if (seekPerception != null)
        {
            var gameObjects = seekPerception.GetGameObjects();
            if (gameObjects.Length > 0)
            {
                Vector3 force = Seek(gameObjects[0]);
                movement.ApplyForce(force);
            }
        }

        if (fleePerception != null)
        {
            var gameObjects = fleePerception.GetGameObjects();
            if (gameObjects.Length > 0)
            {
                Vector3 force = Flee(gameObjects[0]);
                movement.ApplyForce(force);
            }
        }

        if (!hasTarget)
        {
            Vector3 force = Wander();
            movement.ApplyForce(force);
        }

        // Wrap AFTER movement has updated position (your movement integrates in LateUpdate)
        // so ideally move this into LateUpdate, but leaving it here still works visually.
        transform.position = Utilities.Wrap(
            transform.position,
            new Vector3(-15, -15, -15),
            new Vector3(15, 15, 15)
        );

        // Face movement direction
        if (movement.Velocity.sqrMagnitude > 0.0001f)
        {
            // Ground-only facing (optional)
            Vector3 v = movement.Velocity;
            v.y = 0f;

            if (v.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(v.normalized, Vector3.up);
        }
    }

    Vector3 Seek(GameObject go)
    {
        Vector3 direction = go.transform.position - transform.position;
        return GetSteeringForce(direction);
    }

    Vector3 Flee(GameObject go)
    {
        Vector3 direction = transform.position - go.transform.position;
        return GetSteeringForce(direction);
    }

    Vector3 GetSteeringForce(Vector3 direction)
    {
        // If you're moving on the ground, ignore Y
        direction.y = 0f;

        Vector3 desired = direction.normalized * movement.maxSpeed;
        Vector3 steer = desired - movement.Velocity;
        return Vector3.ClampMagnitude(steer, movement.maxForce);
    }

    private Vector3 Wander()
    {
        // randomly adjust the wander angle within (+/-) displacement range 
        wanderAngle += Random.Range(-wanderDisplacement, wanderDisplacement);

        // calculate a point on the wander circle using the wander angle
        Quaternion rotation = Quaternion.AngleAxis(wanderAngle, Vector3.up);
        Vector3 pointOnCircle = rotation * (Vector3.forward * wanderRadius);

        // project the wander circle in front of the agent 
        Vector3 circleCenter = movement.Velocity.normalized * wanderDistance;

        // steer toward the target point (circle center + point on circle) 
        Vector3 force = GetSteeringForce(circleCenter + pointOnCircle);

        Debug.DrawLine(transform.position, transform.position + circleCenter, Color.blue);
        Debug.DrawLine(transform.position, transform.position + circleCenter + pointOnCircle, Color.red);

        return force;
    }
}
