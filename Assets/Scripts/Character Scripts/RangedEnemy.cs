using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class RangedEnemy : Enemy
{
    [SerializeField] LayerMask layermask;
    float shotTimer = 2;
    public Weapon weapon;
   

    // Update is called once per frame
    void Update()
    {
        
    }
    protected override void EnemyBehaviour()
    {
        CheckLineOfSight();
        switch (state)
        {
            case States.CHASE:
                Move();
                break;
            case States.ATTACK:
                transform.rotation = Quaternion.LookRotation(target.transform.position - (transform.position + Vector3.down), Vector3.up);
                shotTimer -= Time.deltaTime;
                if (shotTimer <= 0)
                {
                    //weapon.SetFireInput(true);
                    Debug.Log("EnemyFired");
                    shotTimer = 2;
                }
                break;
        }
    }

    void CheckLineOfSight()
    {
        if (Physics.Linecast(transform.position, target.transform.position,out RaycastHit hit, layermask, QueryTriggerInteraction.Ignore))
        {
            if (hit.rigidbody && hit.rigidbody.TryGetComponent(out Character player))
            {
                Debug.Log("found");
                agent.enabled = false;
                state = States.ATTACK;
            }
            else
            {
                agent.enabled = true;
                state = States.CHASE;
            }
        }
    }
}
