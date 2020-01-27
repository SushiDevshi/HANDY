using EntityStates;
using RoR2;
using RoR2.CharacterAI;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace HANDY.HAND
{
    public class OVERCLOCK : BaseState
    {
        public float baseDuration = 0.25f;
        public override void OnEnter()
        {
            base.OnEnter();
            if (NetworkServer.active && base.isAuthority)
            {
                base.GetComponent<HANDOverclockController>().EnableOverclock();
                new BlastAttack
                {
                    attacker = base.gameObject,
                    inflictor = base.gameObject,
                    teamIndex = TeamComponent.GetObjectTeam(base.gameObject),
                    baseDamage = this.damageStat * 2,
                    baseForce = 5,
                    position = base.modelLocator.transform.position,
                    radius = 20,
                    procCoefficient = 0f,
                    falloffModel = BlastAttack.FalloffModel.None,
                    damageType = DamageType.Stun1s,
                    crit = RollCrit()
                }.Fire();
            }
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (base.fixedAge > this.baseDuration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
                return;
            }
        }
        private Ray GetNextOrbRay()
        {
            Ray r = new Ray();

            r.origin = base.modelLocator.transform.position;
            r.direction = Vector3.up;

            RaycastHit rh;

            if (Physics.Raycast(base.modelLocator.transform.position, Vector3.down, out rh, 1000f, LayerIndex.world.mask, QueryTriggerInteraction.UseGlobal))
            {
                r.direction = rh.normal;
                r.origin = rh.point;
            }

            return r;
        }
    }
}
