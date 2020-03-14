using System;
using System.Collections.Generic;
using System.Linq;
using EntityStates;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace HANDY.Weapon
{
    public class SLAM : BaseState
    {
        public float baseDuration = 1.2f;
        public float returnToIdlePercentage;
        public float impactDamageCoefficient = 5f;
        public float earthquakeDamageCoefficient = 4f;
        public float forceMagnitude = 32f;
        public float radius = 6f;//sunder
        public static GameObject hitEffectPrefab = Resources.Load<GameObject>("prefabs/effects/omnieffect/omniimpactvfx");
        public GameObject swingEffectPrefab = Resources.Load<GameObject>("prefabs/effects/handslamtrail");
        public GameObject projectilePrefab = Resources.Load<GameObject>("prefabs/projectiles/sunder");
        private Transform hammerChildTransform;
        private ExtendedOverlapAttack attack;
        private Animator modelAnimator;
        private float duration;
        private bool hasSwung;
        private float hitPauseDuration = 0.1f;
        private bool enteredHitPause = false;
        private bool exitedHitPause = false;
        public float shorthopVelocityFromHit = 8f;
        private float hitPauseTimer = 0f;
        private Vector3 storedVelocity;
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
                this.attack.damage = this.impactDamageCoefficient * this.damageStat;
                this.attack.isCrit = RollCrit();

                if (base.GetComponent<HANDOverclockController>().overclockOn && Util.CheckRoll(30, base.characterBody.master))
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
                    ProjectileManager.instance.FireProjectile(this.projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, this.damageStat * this.earthquakeDamageCoefficient, this.forceMagnitude, RollCrit(), DamageColorIndex.Default, null, -1f);
                    var enemies = CollectEnemies(3, base.transform.position + base.characterDirection.forward * 2f, 3f);
                    if (CheckCollider(enemies))
                    {
                        BeginHitPause();
                    }
                    if (SLAM.hitEffectPrefab)
                    {
                        EffectManager.SimpleImpactEffect(SLAM.hitEffectPrefab, aimRay.origin, aimRay.origin, true);
                    }
                    else
                    {
                        EffectManager.SpawnEffect(SLAM.hitEffectPrefab, new EffectData
                        {
                            origin = aimRay.origin,
                            scale = 10
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
                var enemies = CollectEnemies(3, base.transform.position + base.characterDirection.forward * 2f, 3f);
                if (base.isAuthority && this.enteredHitPause && this.hitPauseTimer > 0f && (CheckCollider(enemies)))
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
        private bool CheckCollider(Collider[] array)
        {
            // now that we have our enemies, only get the ones within the Y dimension
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

        private Collider[] CollectEnemies(float radius, Vector3 position, float maxYDiff)
        {
            Collider[] array = Physics.OverlapSphere(position, radius, LayerIndex.entityPrecise.mask);
            array = array.Where(x => Mathf.Abs(x.ClosestPoint(base.transform.position).y - base.transform.position.y) <= maxYDiff).ToArray();
            return array;
        }
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}
