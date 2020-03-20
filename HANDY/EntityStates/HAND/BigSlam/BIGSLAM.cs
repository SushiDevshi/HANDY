using System;
using System.Collections.Generic;
using System.Linq;
using EntityStates;
using RoR2;
using UnityEngine;

namespace HANDY.Weapon
{
    public class BIGSLAM : BaseState
    {
        public float attackDamageCoefficient = 4f;
        public float blastDamageCoefficient = 4f;
        public float baseDuration = 1.5f;
        public float forceMagnitude = 32f;
        public float radius = 6f;
        private float duration;

        public static GameObject notificationEffectPrefab = Resources.Load<GameObject>("prefabs/effects/omnieffect/omniimpactvfx");
        public static GameObject rumbleEffectPrefab = Resources.Load<GameObject>("prefabs/effects/impacteffects/beetlequeendeathimpact");
        public GameObject swingEffectPrefab = Resources.Load<GameObject>("prefabs/effects/muzzleflashes/muzzleflashloader");


        private Animator modelAnimator;
        private Transform hammerChildTransform;

        public ExtendedOverlapAttack attack;
        public BlastAttack blastAttack;

        public float hitPauseDuration = 0.1f;
        public float shorthopVelocityFromHit = 8f;
        public float hitPauseTimer = 0f;

        private Vector3 storedVelocity;

        private bool hasSwung;
        private bool enteredHitPause = false;
        private bool exitedHitPause = false;
        public override void OnEnter()
        {
            base.OnEnter();
            if (base.isAuthority)
            {
                this.duration = this.baseDuration / base.attackSpeedStat;
                this.modelAnimator = base.GetModelAnimator();
                Transform modelTransform = base.GetModelTransform();
                this.attack = new ExtendedOverlapAttack();
                this.attack.attacker = base.gameObject;
                this.attack.inflictor = base.gameObject;
                this.attack.teamIndex = TeamComponent.GetObjectTeam(this.attack.attacker);
                this.attack.damage = this.attackDamageCoefficient * this.damageStat;
                this.attack.isCrit = RollCrit();
                this.attack.damageType = DamageType.Stun1s;
                this.blastAttack = new BlastAttack();

                this.blastAttack.attacker = base.gameObject;
                this.blastAttack.inflictor = base.gameObject;
                this.blastAttack.teamIndex = TeamComponent.GetObjectTeam(base.gameObject);
                this.blastAttack.baseDamage = this.damageStat * this.blastDamageCoefficient;
                this.blastAttack.baseForce = 16;
                this.blastAttack.position = base.transform.position;
                this.blastAttack.radius = 15;
                this.blastAttack.procCoefficient = 1f;
                this.blastAttack.falloffModel = BlastAttack.FalloffModel.None;
                this.blastAttack.damageType = DamageType.Stun1s;
                this.blastAttack.crit = RollCrit();

                if (base.GetComponent<HANDOverclockController>().overclockOn && Util.CheckRoll(30, base.characterBody.master))
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
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (base.isAuthority && this.modelAnimator && this.modelAnimator.GetFloat("Hammer.hitBoxActive") > 0.5f)
            {
                if (!this.hasSwung)
                {
                    Ray aimRay = base.GetAimRay();
                    EffectManager.SimpleMuzzleFlash(this.swingEffectPrefab, base.gameObject, "SwingCenter", true);
                    blastAttack.Fire();
 
                    if (CheckIfAttackHit(3, base.transform.position + base.characterDirection.forward * 2f, 3f))
                    {
                        BeginHitPause();
                    }
                    if (BIGSLAM.notificationEffectPrefab)
                    {
                        EffectManager.SpawnEffect(BIGSLAM.notificationEffectPrefab, new EffectData
                        {
                            origin = aimRay.origin,
                            scale = 15
                        }, true);
                    }
                    else
                    {
                        EffectManager.SpawnEffect(BIGSLAM.notificationEffectPrefab, new EffectData
                        {
                            origin = aimRay.origin,
                            scale = 15
                        }, true);
                    }
                    if (BIGSLAM.rumbleEffectPrefab)
                    {
                        EffectManager.SpawnEffect(BIGSLAM.rumbleEffectPrefab, new EffectData
                        {
                            origin = aimRay.origin,
                            scale = 15
                        }, true);
                    }
                    else
                    {
                        EffectManager.SpawnEffect(BIGSLAM.rumbleEffectPrefab, new EffectData
                        {
                            origin = aimRay.origin,
                            scale = 15
                        }, true);
                    }
                    Util.PlaySound("Play_MULT_shift_hit", base.gameObject);
                    this.hasSwung = true;
                }
                this.attack.forceVector = this.hammerChildTransform.right * this.forceMagnitude;
                this.attack.Fire(null);
            }
            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
                return;
            }
            else
            {
                if (base.isAuthority && this.enteredHitPause && this.hitPauseTimer > 0f && CheckIfAttackHit(3, base.transform.position + base.characterDirection.forward * 2f, 3f))
                {
                    this.hitPauseTimer -= Time.fixedDeltaTime;
                    base.characterMotor.velocity = Vector3.zero;
                    if (this.hitPauseTimer <= 0f)
                    {
                        this.ExitHitPause();
                    }
                }
            }
        }
        private void BeginHitPause()
        {
            if (!base.characterMotor.isFlying)
            {
                this.enteredHitPause = true;
                this.storedVelocity = base.characterMotor.velocity;
                base.characterMotor.velocity = Vector3.zero;
                this.hitPauseTimer = this.hitPauseDuration;
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

        private void ExitHitPause()
        {
            this.hitPauseTimer = 0f;
            if (!base.isGrounded)
            {
                this.storedVelocity.y = Mathf.Max(this.storedVelocity.y, this.shorthopVelocityFromHit);
            }
            base.characterMotor.velocity = this.storedVelocity;
            this.storedVelocity = Vector3.zero;
            this.exitedHitPause = true;
        }


        public override void OnExit()
        {
            if (!this.hasSwung)
            {
                if (this.enteredHitPause)
                {
                    this.ExitHitPause();
                }
            }
            if (this.enteredHitPause && !this.exitedHitPause)
            {
                this.ExitHitPause();
            }
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}

