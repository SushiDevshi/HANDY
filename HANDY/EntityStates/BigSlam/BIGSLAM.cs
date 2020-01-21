using System;
using EntityStates;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace HANDY.Weapon
{
    public class BIGSLAM : BaseState
    {
        public float baseDuration = 1.2f;
        public float returnToIdlePercentage;
        public float impactDamageCoefficient = 6f;
        public float forceMagnitude = 64;
        public float radius = 12f;//sunder
        public GameObject hitEffectPrefab = Resources.Load<GameObject>("prefabs/effects/impacteffects/beetleguardgroundslam");//;
        public GameObject projectilePrefab = Resources.Load<GameObject>("prefabs/projectiles/sunder");
        private Transform hammerChildTransform;
        private OverlapAttack attack;
        private Animator modelAnimator;
        private float duration;
        private bool hasSwung;
        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = this.baseDuration / base.attackSpeedStat;
            this.modelAnimator = base.GetModelAnimator();
            Transform modelTransform = base.GetModelTransform();
            this.attack = new OverlapAttack();
            this.attack.attacker = base.gameObject;
            this.attack.inflictor = base.gameObject;
            this.attack.teamIndex = TeamComponent.GetObjectTeam(this.attack.attacker);
            this.attack.damage = this.impactDamageCoefficient * this.damageStat;
            this.attack.hitEffectPrefab = this.hitEffectPrefab;
            this.attack.isCrit = RollCrit();
            if (base.GetComponent<HANDOverclockController>().overclockOn)
            {
                this.attack.damageType = DamageType.IgniteOnHit;
            }
            if (modelTransform)
            {
                this.attack.hitBoxGroup = Array.Find<HitBoxGroup>(modelTransform.GetComponents<HitBoxGroup>(), (HitBoxGroup element) => element.groupName == "Hammer");
                ChildLocator component = modelTransform.GetComponent<ChildLocator>();
                if (component)
                {
                    this.hammerChildTransform = component.FindChild("SwingCenter");
                }
            }   
            if (this.modelAnimator)
            {
                base.PlayAnimation("Gesture", "Slam", "Slam.playbackRate", this.duration);
            }
            if (base.characterBody)
            {
                base.characterBody.SetAimTimer(2f);
            }
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (NetworkServer.active && this.modelAnimator && this.modelAnimator.GetFloat("Hammer.hitBoxActive") > 0.5f)
            {
                if (!this.hasSwung)
                {
                    Ray aimRay = base.GetAimRay();
                    BlastAttack blastattack = new BlastAttack();
                    blastattack.position = aimRay.origin;
                    blastattack.radius = 12;
                    blastattack.damageType = DamageType.Stun1s;
                    blastattack.baseForce = 12;
                    blastattack.crit = RollCrit();

                    if (base.GetComponent<HANDOverclockController>().overclockOn)
                    {
                        blastattack.radius = 24;
                        blastattack.damageType = DamageType.IgniteOnHit;
                        blastattack.baseForce = 24;
                    }

                    blastattack.Fire();
                    this.hasSwung = true;
                }
                this.attack.forceVector = this.hammerChildTransform.right;
                this.attack.Fire(null);
            }
            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
                return;
            }
        }
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}

