using System;
using System.Collections.Generic;
using System.Linq;
using EntityStates;
using EntityStates.HAND;
using EntityStates.HAND.Weapon;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace HANDY.Weapon
{
    public class HURT : BaseState
    {
        public static float baseDuration = 1.3f;
        public float returnToIdlePercentage = FullSwing.returnToIdlePercentage;
        public float damageCoefficient = 4f;
        public float forceMagnitude = 1250f;
        public float radius = 12f;
        public float duration;
        public bool hasSwung;
        public bool hasHit;

        public Transform hammerChildTransform;
        public ExtendedOverlapAttack attack;
        public Animator modelAnimator;

        public float hitPauseDuration = 0.1f;
        public bool enteredHitPause = false;
        public bool exitedHitPause = false;
        public float shorthopVelocityFromHit = 8f;
        private float hitPauseTimer = 0f;
        private Vector3 storedVelocity;

        public static GameObject hitEffectPrefab = Resources.Load<GameObject>("prefabs/effects/omnieffect/omniimpactvfxmedium");
        public GameObject swingEffectPrefab = Resources.Load<GameObject>("prefabs/effects/handslamtrail");
        public override void OnEnter()
        {
            base.OnEnter();
            if (base.isAuthority)
            {
                this.duration = HURT.baseDuration / this.attackSpeedStat;
                this.modelAnimator = base.GetModelAnimator();
                Transform modelTransform = base.GetModelTransform();

                this.attack = new ExtendedOverlapAttack();
                this.attack.attacker = base.gameObject;
                this.attack.inflictor = base.gameObject;
                this.attack.teamIndex = TeamComponent.GetObjectTeam(this.attack.attacker);
                this.attack.damage = damageCoefficient * this.damageStat;
                this.attack.hitEffectPrefab = HURT.hitEffectPrefab;
                this.attack.pushAwayForce = this.forceMagnitude;
                this.attack.procCoefficient = 1;
                this.attack.upwardsForce = this.forceMagnitude;
                this.attack.isCrit = RollCrit();

                if (base.GetComponent<HANDOverclockController>().overclockOn && Util.CheckRoll(30, base.characterBody.master) && base.isAuthority)
                {
                    this.attack.damageType = DamageType.Stun1s;
                }

                if (modelTransform && base.isAuthority)
                {
                    this.attack.hitBoxGroup = Array.Find<HitBoxGroup>(modelTransform.GetComponents<HitBoxGroup>(), (HitBoxGroup element) => element.groupName == "Hammer");
                    ChildLocator component = modelTransform.GetComponent<ChildLocator>();
                    if (component)
                    {
                        this.hammerChildTransform = component.FindChild("SwingCenter");
                    }
                }
                if (this.modelAnimator && base.isAuthority)
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
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (base.isAuthority && this.modelAnimator && this.modelAnimator.GetFloat("Hammer.hitBoxActive") > 0.5f)
            {
                if (!this.hasSwung)
                {
                    Ray aimRay = base.GetAimRay();
                    this.hasSwung = true;
                    EffectManager.SimpleMuzzleFlash(this.swingEffectPrefab, base.gameObject, "SwingCenter", true);
                    if (CheckIfAttackHit(3, base.transform.position + base.characterDirection.forward * 2f, 1f) && base.isAuthority)
                    {
                        EnterHitPauseState();
                    }
                    Util.PlaySound("Play_MULT_shift_hit", this.gameObject);
                    //PullEnemies(aimRay.origin, aimRay.direction, 30, 30000, 3000, TeamIndex.Player);
                }
                this.attack.Fire(null);
                this.attack.forceVector = this.hammerChildTransform.right * this.forceMagnitude;
            }
            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
                return;
            }
            else
            {
                if (base.isAuthority && this.enteredHitPause && this.hitPauseTimer > 0f && (CheckIfAttackHit(3, base.transform.position + base.characterDirection.forward * 2f, 3f)))
                {
                    this.hitPauseTimer -= Time.fixedDeltaTime;
                    base.characterMotor.velocity = Vector3.zero;
                    if (this.hitPauseTimer <= 0f)
                    {
                        this.ExitHitPauseState();
                    }
                }
            }
        }
        private bool CheckIfAttackHit(float radius, Vector3 position, float maxYDiff)
        {
            Collider[] array = Physics.OverlapSphere(position, radius, LayerIndex.entityPrecise.mask);
            array = array.Where(x => Mathf.Abs(x.ClosestPoint(base.transform.position).y - base.transform.position.y) <= maxYDiff).ToArray();
            var hurtboxes = array.Where(x => x.GetComponent<HurtBox>() != null);
            List<HurtBoxGroup> allReadyDamaged = new List<HurtBoxGroup>();
            foreach (var hurtBox in hurtboxes)
            {
                var hurtBox2 = hurtBox.GetComponentInChildren<HurtBox>();
                if (hurtBox2 == null) continue;
                if (allReadyDamaged.Where(x => x == hurtBox2.hurtBoxGroup).Count() > 0) continue; // already hit them
                if (hurtBox2.teamIndex == base.teamComponent.teamIndex) continue; // dont hit teammates LUL
                allReadyDamaged.Add(hurtBox2.hurtBoxGroup);
            }
            if (allReadyDamaged == null) return false;
            return allReadyDamaged.Count() > 0;
        }



        protected virtual void ExitHitPauseState()
        {
            if (base.isAuthority)
            {
                this.hitPauseTimer = 0f;
                if (!base.isGrounded)
                {
                    this.storedVelocity.y = Mathf.Max(this.storedVelocity.y, this.shorthopVelocityFromHit);
                }
                base.characterMotor.velocity = this.storedVelocity;
                this.storedVelocity = Vector3.zero;
                this.exitedHitPause = true;
                //if (this.modelAnimator)
                //{
                //    this.modelAnimator.speed = 1f;
                //}
            }
        }
        private void EnterHitPauseState()
        {
            if (!base.characterMotor.isFlying && base.isAuthority)
            {
                this.enteredHitPause = true;
                this.storedVelocity = base.characterMotor.velocity;
                base.characterMotor.velocity = Vector3.zero;
                this.hitPauseTimer = this.hitPauseDuration;
                //if (this.modelAnimator)
                //{
                //    this.modelAnimator.speed = 0f;
                //}
            }
        }

        public override void OnExit()
        {
            if (base.isAuthority)
            {
                if (!this.hasSwung)
                {
                    if (this.enteredHitPause)
                    {
                        this.ExitHitPauseState();
                    }
                }
                if (this.enteredHitPause && !this.exitedHitPause)
                {
                    this.ExitHitPauseState();
                }
                base.OnExit();
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
        private static void PullEnemies(Vector3 position, Vector3 direction, float coneAngle, float maxDistance, float force, TeamIndex excludedTeam)
        {
            float num = Mathf.Cos(coneAngle * 0.5f * 0.017453292f);
            foreach (Collider collider in Physics.OverlapSphere(position, maxDistance))
            {
                Vector3 position2 = collider.transform.position;
                Vector3 normalized = (position - position2).normalized;
                if (Vector3.Dot(-normalized, direction) >= num)
                {
                    if (collider.GetComponent<TeamComponent>())
                    {
                        TeamComponent component = collider.GetComponent<TeamComponent>();
                        TeamIndex teamIndex = component.teamIndex;
                        if (teamIndex != excludedTeam)
                        {
                            if (collider.GetComponent<CharacterMotor>())
                            {
                                CharacterMotor component2 = collider.GetComponent<CharacterMotor>();
                                component2.ApplyForce(normalized * force, false, false);
                            }
                            if (collider.GetComponent<Rigidbody>())
                            {
                                Rigidbody component3 = collider.GetComponent<Rigidbody>();
                                component3.AddForce(normalized * force, ForceMode.Impulse);
                            }
                        }
                    }
                }
            }
        }
    }
}
