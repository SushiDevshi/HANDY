using System;
using EntityStates;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace HANDY.Weapon
{
    public class HURT : BaseState
    {
        public static float baseDuration = 1.5f;
        public float returnToIdlePercentage = EntityStates.HAND.Weapon.FullSwing.returnToIdlePercentage;
        public float damageCoefficient = 4.5f;
        public float forceMagnitude = 32f;
        public float radius = 12f;
        public GameObject hitEffectPrefab = Resources.Load<GameObject>("prefabs/effects/omnieffect/omniimpactvfx");
        private Transform hammerChildTransform;
        private OverlapAttack attack;
        private Animator modelAnimator;
        private float duration;
        private bool hasSwung;

        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = HURT.baseDuration / this.attackSpeedStat;
            this.modelAnimator = base.GetModelAnimator();
            Transform modelTransform = base.GetModelTransform();
            this.attack = new OverlapAttack();
            this.attack.attacker = base.gameObject;
            this.attack.inflictor = base.gameObject;
            this.attack.teamIndex = TeamComponent.GetObjectTeam(this.attack.attacker);
            this.attack.damage = damageCoefficient * this.damageStat;
            this.attack.hitEffectPrefab = this.hitEffectPrefab;
            this.attack.isCrit = RollCrit();
            if (base.GetComponent<HANDOverclockController>().overclockOn)
            {
                this.attack.damageType = DamageType.Stun1s;
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
                int layerIndex = this.modelAnimator.GetLayerIndex("Gesture");
                if (this.modelAnimator.GetCurrentAnimatorStateInfo(layerIndex).IsName("FullSwing3") || this.modelAnimator.GetCurrentAnimatorStateInfo(layerIndex).IsName("FullSwing1"))
                {
                    base.PlayCrossfade("Gesture", "FullSwing2", "FullSwing.playbackRate", this.duration / (1f - this.returnToIdlePercentage), 0.2f);
                }
                else if (this.modelAnimator.GetCurrentAnimatorStateInfo(layerIndex).IsName("FullSwing2"))
                {
                    base.PlayCrossfade("Gesture", "FullSwing3", "FullSwing.playbackRate", this.duration / (1f - this.returnToIdlePercentage), 0.2f);
                }
                else
                {
                    base.PlayCrossfade("Gesture", "FullSwing1", "FullSwing.playbackRate", this.duration / (1f - this.returnToIdlePercentage), 0.2f);
                }
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
                    this.hasSwung = true;
                    Util.PlaySound("Play_MULT_shift_hit", this.gameObject);
                }
                this.attack.forceVector = this.hammerChildTransform.right * this.forceMagnitude;
                this.attack.Fire(null);
            }
            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
                return;
            }



        }
        // Token: 0x06002FF6 RID: 12278 RVA: 0x0000BDAE File Offset: 0x00009FAE
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }

        // Token: 0x06002FF7 RID: 12279 RVA: 0x000CDA7C File Offset: 0x000CBC7C
        private static void PullEnemies(Vector3 position, Vector3 direction, float coneAngle, float maxDistance, float force, TeamIndex excludedTeam)
        {
            float num = Mathf.Cos(coneAngle * 0.5f * 0.017453292f);
            foreach (Collider collider in Physics.OverlapSphere(position, maxDistance))
            {
                Vector3 position2 = collider.transform.position;
                Vector3 normalized = (position - position2).normalized;
                if (Vector3.Dot(-normalized, direction) >= num)
                {
                    TeamComponent component = collider.GetComponent<TeamComponent>();
                    if (component)
                    {
                        TeamIndex teamIndex = component.teamIndex;
                        if (teamIndex != excludedTeam)
                        {
                            CharacterMotor component2 = collider.GetComponent<CharacterMotor>();
                            if (component2)
                            {
                                component2.ApplyForce(normalized * force, false, false);
                            }
                            Rigidbody component3 = collider.GetComponent<Rigidbody>();
                            if (component3)
                            {
                                component3.AddForce(normalized * force, ForceMode.Impulse);
                            }
                        }
                    }
                }
            }
        }

        // Token: 0x04002DC3 RID: 11715

    }
}
