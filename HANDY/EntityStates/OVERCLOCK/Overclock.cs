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
        public static GameObject hitEffectPrefab = Resources.Load<GameObject>("prefabs/effects/omnieffect/omniimpactvfx");
        public override void OnEnter()
        {
            base.OnEnter();
            if (base.isAuthority)
            {
                base.GetComponent<HANDOverclockController>().EnableOverclock();
                new BlastAttack
                {
                    attacker = base.gameObject,
                    inflictor = base.gameObject,
                    teamIndex = TeamComponent.GetObjectTeam(base.gameObject),
                    baseDamage = this.damageStat * 1.5f,
                    baseForce = -30,
                    position = base.modelLocator.transform.position,
                    radius = 35,
                    procCoefficient = 0f,
                    falloffModel = BlastAttack.FalloffModel.None,
                    damageType = DamageType.Stun1s,
                    crit = RollCrit()
                }.Fire();
                if (OVERCLOCK.hitEffectPrefab)
                {
                    EffectManager.SpawnEffect(OVERCLOCK.hitEffectPrefab, new EffectData
                    {
                        origin = base.transform.position,
                        scale = 15
                    }, false);
                }
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
