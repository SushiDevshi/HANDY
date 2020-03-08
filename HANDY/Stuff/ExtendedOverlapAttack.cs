using System;
using System.Collections.Generic;
using HANDY;
using RoR2;
using RoR2.Audio;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace HANDY
{
    public class ExtendedOverlapAttack
    {
        public Vector3 lastFireAverageHitPosition { get; private set; }

        private bool HurtBoxPassesFilter(HurtBox hurtBox)
        {
            if (!hurtBox.healthComponent)
            {
                return true;
            }
            if (this.hitBoxGroup.transform.IsChildOf(hurtBox.healthComponent.transform))
            {
                return false;
            }
            if (this.ignoredHealthComponentList.Contains(hurtBox.healthComponent))
            {
                return false;
            }
            TeamComponent component = hurtBox.healthComponent.GetComponent<TeamComponent>();
            return !component || component.teamIndex != this.teamIndex;
        }
        public bool Fire(List<HealthComponent> hitResults = null)
        {
            if (!this.hitBoxGroup)
            {
                return false;
            }
            HitBox[] hitBoxes = this.hitBoxGroup.hitBoxes;
            for (int i = 0; i < hitBoxes.Length; i++)
            {
                Transform transform = hitBoxes[i].transform;
                Vector3 position = transform.position;
                Vector3 vector = transform.lossyScale * 0.5f;
                Quaternion rotation = transform.rotation;
                if (float.IsInfinity(vector.x) || float.IsInfinity(vector.y) || float.IsInfinity(vector.z))
                {
                    Chat.AddMessage("Aborting ExtendedOverlapAttack.Fire: hitBoxHalfExtents are infinite.");
                }
                else if (float.IsNaN(vector.x) || float.IsNaN(vector.y) || float.IsNaN(vector.z))
                {
                    Chat.AddMessage("AbortingExtendedOverlapAttack.Fire: hitBoxHalfExtents are NaN.");
                }
                else if (float.IsInfinity(position.x) || float.IsInfinity(position.y) || float.IsInfinity(position.z))
                {
                    Chat.AddMessage("Aborting ExtendedOverlapAttack.Fire: hitBoxCenter is infinite.");
                }
                else if (float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z))
                {
                    Chat.AddMessage("Aborting ExtendedOverlapAttack.Fire: hitBoxCenter is NaN.");
                }
                else if (float.IsInfinity(rotation.x) || float.IsInfinity(rotation.y) || float.IsInfinity(rotation.z) || float.IsInfinity(rotation.w))
                {
                    Chat.AddMessage("Aborting ExtendedOverlapAttack.Fire: hitBoxRotation is infinite.");
                }
                else if (float.IsNaN(rotation.x) || float.IsNaN(rotation.y) || float.IsNaN(rotation.z) || float.IsNaN(rotation.w))
                {
                    Chat.AddMessage("Aborting ExtendedOverlapAttack.Fire: hitBoxRotation is NaN.");
                }
                else
                {
                    Collider[] array = Physics.OverlapBox(position, vector, rotation, LayerIndex.entityPrecise.mask);
                    int num = array.Length;
                    int num2 = 0;
                    for (int j = 0; j < num; j++)
                    {
                        HurtBox component = array[j].GetComponent<HurtBox>();
                        if (component && this.HurtBoxPassesFilter(component))
                        {
                            Vector3 position2 = component.transform.position;
                            this.overlapList.Add(new ExtendedOverlapAttack.OverlapInfo
                            {
                                hurtBox = component,
                                hitPosition = position2,
                                pushDirection = (position2 - position).normalized
                            });
                            this.ignoredHealthComponentList.Add(component.healthComponent);
                            if (hitResults != null)
                            {
                                hitResults.Add(component.healthComponent);
                            }
                            num2++;
                        }

                        if (num2 >= this.maximumOverlapTargets)
                        {
                            break;
                        }
                    }
                }
            }
            this.ProcessHits(this.overlapList);
            this.overlapList.Clear();
            return this.overlapList.Count > 0;
        }

        [NetworkMessageHandler(msgType = 7595, client = false, server = true)]
        public static void HandleExtendedOverlapAttackHits(NetworkMessage netMsg)
        {
            netMsg.ReadMessage<ExtendedOverlapAttack.ExtendedOverlapAttackMessage>(ExtendedOverlapAttack.incomingMessage);
            ExtendedOverlapAttack.PerformDamage(ExtendedOverlapAttack.incomingMessage.attacker, ExtendedOverlapAttack.incomingMessage.inflictor, ExtendedOverlapAttack.incomingMessage.damage, ExtendedOverlapAttack.incomingMessage.isCrit, ExtendedOverlapAttack.incomingMessage.procChainMask, ExtendedOverlapAttack.incomingMessage.procCoefficient, ExtendedOverlapAttack.incomingMessage.damageColorIndex, ExtendedOverlapAttack.incomingMessage.damageType, ExtendedOverlapAttack.incomingMessage.forceVector, ExtendedOverlapAttack.incomingMessage.pushAwayForce, ExtendedOverlapAttack.incomingMessage.upwardsForce, ExtendedOverlapAttack.incomingMessage.overlapInfoList);
        }

        private void ProcessHits(List<ExtendedOverlapAttack.OverlapInfo> hitList)
        {
            if (hitList.Count == 0)
            {
                return;
            }
            Vector3 vector = Vector3.zero;
            float d = 1f / (float)hitList.Count;
            for (int i = 0; i < hitList.Count; i++)
            {
                ExtendedOverlapAttack.OverlapInfo overlapInfo = hitList[i];
                if (this.hitEffectPrefab)
                {
                    Vector3 forward = -hitList[i].pushDirection;
                    EffectManager.SpawnEffect(this.hitEffectPrefab, new EffectData
                    {
                        origin = overlapInfo.hitPosition,
                        rotation = Util.QuaternionSafeLookRotation(forward),
                    }, true);

                    Util.PlaySound("Play_MULT_shift_hit", this.hitEffectPrefab.gameObject);
                }
                vector += overlapInfo.hitPosition * d;
                SurfaceDefProvider component = hitList[i].hurtBox.GetComponent<SurfaceDefProvider>();
                if (component && component.surfaceDef)
                {
                    SurfaceDef objectSurfaceDef = SurfaceDefProvider.GetObjectSurfaceDef(hitList[i].hurtBox.collider, hitList[i].hitPosition);
                    if (objectSurfaceDef)
                    {
                        if (objectSurfaceDef.impactEffectPrefab)
                        {
                            EffectManager.SpawnEffect(objectSurfaceDef.impactEffectPrefab, new EffectData
                            {
                                origin = overlapInfo.hitPosition,
                                rotation = ((overlapInfo.pushDirection == Vector3.zero) ? Quaternion.identity : Util.QuaternionSafeLookRotation(overlapInfo.pushDirection)),
                                color = objectSurfaceDef.approximateColor,
                                scale = 2f
                            }, true);
                        }
                        if (objectSurfaceDef.impactSoundString != null && objectSurfaceDef.impactSoundString.Length != 0)
                        {
                            Util.PlaySound(objectSurfaceDef.impactSoundString, hitList[i].hurtBox.gameObject);
                        }
                    }
                }
            }
            this.lastFireAverageHitPosition = vector;
            //Since we're the server, we can do damage pretty freely.
            if (NetworkServer.active)
            {
                ExtendedOverlapAttack.PerformDamage(this.attacker, this.inflictor, this.damage, this.isCrit, this.procChainMask, this.procCoefficient, this.damageColorIndex, this.damageType, this.forceVector, this.pushAwayForce, this.upwardsForce, hitList);
                return;
            }
            ExtendedOverlapAttack.outgoingMessage.attacker = this.attacker;
            ExtendedOverlapAttack.outgoingMessage.inflictor = this.inflictor;
            ExtendedOverlapAttack.outgoingMessage.damage = this.damage;
            ExtendedOverlapAttack.outgoingMessage.isCrit = this.isCrit;
            ExtendedOverlapAttack.outgoingMessage.procChainMask = this.procChainMask;
            ExtendedOverlapAttack.outgoingMessage.procCoefficient = this.procCoefficient;
            ExtendedOverlapAttack.outgoingMessage.damageColorIndex = this.damageColorIndex;
            ExtendedOverlapAttack.outgoingMessage.damageType = this.damageType;
            ExtendedOverlapAttack.outgoingMessage.forceVector = this.forceVector;
            ExtendedOverlapAttack.outgoingMessage.pushAwayForce = this.pushAwayForce;
            ExtendedOverlapAttack.outgoingMessage.upwardsForce = this.upwardsForce;

            Util.CopyList<ExtendedOverlapAttack.OverlapInfo>(hitList, ExtendedOverlapAttack.outgoingMessage.overlapInfoList);
            GameNetworkManager.singleton.client.connection.SendByChannel(7595, ExtendedOverlapAttack.outgoingMessage, QosChannelIndex.defaultReliable.intVal);
        }

        private static void PerformDamage(GameObject attacker, GameObject inflictor, float damage, bool isCrit, ProcChainMask procChainMask, float procCoefficient, DamageColorIndex damageColorIndex, DamageType damageType, Vector3 forceVector, float pushAwayForce, float upwardsForce, List<ExtendedOverlapAttack.OverlapInfo> hitList)
        {
            for (int i = 0; i < hitList.Count; i++)
            {
                ExtendedOverlapAttack.OverlapInfo overlapInfo = hitList[i];
                if (overlapInfo.hurtBox)
                {
                    HealthComponent healthComponent = overlapInfo.hurtBox.healthComponent;
                    if (healthComponent)
                    {
                        DamageInfo damageInfo = new DamageInfo();
                        damageInfo.attacker = attacker;
                        damageInfo.inflictor = inflictor;
                        damageInfo.force = forceVector + pushAwayForce * overlapInfo.pushDirection;
                        damageInfo.damage = damage;
                        damageInfo.crit = isCrit;
                        damageInfo.position = overlapInfo.hitPosition;
                        damageInfo.procChainMask = procChainMask;
                        damageInfo.procCoefficient = procCoefficient;
                        damageInfo.damageColorIndex = damageColorIndex;
                        damageInfo.damageType = damageType;
                        if (healthComponent.gameObject.GetComponent<CharacterBody>().isFlying)
                        {
                            damageInfo.force += new Vector3(0f, upwardsForce, 0f);
                        }
                        if (NetworkServer.active)
                        {
                            //We're the server, and we can do whatever the fug we want with damage!
                            damageInfo.ModifyDamageInfo(overlapInfo.hurtBox.damageModifier);
                            healthComponent.TakeDamage(damageInfo);
                            GlobalEventManager.instance.OnHitEnemy(damageInfo, healthComponent.gameObject);
                            GlobalEventManager.instance.OnHitAll(damageInfo, healthComponent.gameObject);
                        }
                        else
                        {
                            if (ClientScene.ready)
                            {
                                //It's the hosts world, and we're just livin' in it.
                                ExtendedOverlapAttack.write.StartMessage(7595);
                                //Use our own cool channel, else, the game will not recongize it.
                                ExtendedOverlapAttack.write.Write(healthComponent.gameObject);
                                ExtendedOverlapAttack.WriteDamageInfo(ExtendedOverlapAttack.write, damageInfo);
                                ExtendedOverlapAttack.write.Write(healthComponent != null);
                                ExtendedOverlapAttack.write.FinishMessage();
                                ClientScene.readyConnection.SendWriter(ExtendedOverlapAttack.write, QosChannelIndex.defaultReliable.intVal);
                                //Always write on the default reliable intVal, for obvious reasons.
                            }
                        }
                    }
                }
            }
        }

        //Easiest way you can just write damage info, by making a method that does it for you!
        public static void WriteDamageInfo(NetworkWriter writer, DamageInfo damageInfo)
        {
            writer.Write(damageInfo.damage);
            writer.Write(damageInfo.crit);
            writer.Write(damageInfo.attacker);
            writer.Write(damageInfo.inflictor);
            writer.Write(damageInfo.position);
            writer.Write(damageInfo.force);
            writer.Write(damageInfo.procChainMask.mask);
            writer.Write(damageInfo.procCoefficient);
            writer.Write((byte)damageInfo.damageType);
            writer.Write((byte)damageInfo.damageColorIndex);
            writer.Write((byte)(damageInfo.dotIndex + 1));
        }

        public void ResetIgnoredHealthComponents()
        {
            this.ignoredHealthComponentList.Clear();
        }

        public GameObject attacker;

        public GameObject inflictor;

        public TeamIndex teamIndex;

        public Vector3 forceVector;

        public float upwardsForce;

        public float pushAwayForce;

        public float damage = 1f;

        public bool isCrit;

        public ProcChainMask procChainMask;

        public float procCoefficient = 1f;

        public HitBoxGroup hitBoxGroup;

        public GameObject hitEffectPrefab;

        public string impactSound;

        public DamageColorIndex damageColorIndex;

        public DamageType damageType;

        public int maximumOverlapTargets = 100;

        public readonly List<HealthComponent> ignoredHealthComponentList = new List<HealthComponent>();

        public readonly List<ExtendedOverlapAttack.OverlapInfo> overlapList = new List<ExtendedOverlapAttack.OverlapInfo>();

        public static readonly ExtendedOverlapAttack.ExtendedOverlapAttackMessage incomingMessage = new ExtendedOverlapAttack.ExtendedOverlapAttackMessage();

        public static NetworkWriter write = new NetworkWriter();

        public static readonly ExtendedOverlapAttack.ExtendedOverlapAttackMessage outgoingMessage = new ExtendedOverlapAttack.ExtendedOverlapAttackMessage();

        public struct OverlapInfo
        {
            public HurtBox hurtBox;

            public Vector3 hitPosition;

            public Vector3 pushDirection;
        }

        public struct AttackInfo
        {
            public GameObject attacker;

            public GameObject inflictor;

            public float damage;

            public float upwardsForce;

            public bool isCrit;

            public float procCoefficient;

            public DamageColorIndex damageColorIndex;

            public DamageType damageType;

            public Vector3 forceVector;
        }

        public class ExtendedOverlapAttackMessage : MessageBase
        {
            public override void Serialize(NetworkWriter writer)
            {
                base.Serialize(writer);
                writer.Write(this.attacker);
                writer.Write(this.inflictor);
                writer.Write(this.damage);
                writer.Write(this.isCrit);
                writer.Write(this.procChainMask);
                writer.Write(this.procCoefficient);
                writer.Write(this.damageColorIndex);
                writer.Write(this.damageType);
                writer.Write(this.forceVector);
                writer.Write(this.pushAwayForce);
                writer.Write(this.upwardsForce);
                writer.WritePackedUInt32((uint)this.overlapInfoList.Count);
                foreach (ExtendedOverlapAttack.OverlapInfo overlapInfo in this.overlapInfoList)
                {
                    writer.Write(HurtBoxReference.FromHurtBox(overlapInfo.hurtBox));
                    writer.Write(overlapInfo.hitPosition);
                    writer.Write(overlapInfo.pushDirection);
                }
            }
            public override void Deserialize(NetworkReader reader)
            {
                base.Deserialize(reader);
                this.attacker = reader.ReadGameObject();
                this.inflictor = reader.ReadGameObject();
                this.damage = reader.ReadSingle();
                this.isCrit = reader.ReadBoolean();
                this.procChainMask = reader.ReadProcChainMask();
                this.procCoefficient = reader.ReadSingle();
                this.damageColorIndex = reader.ReadDamageColorIndex();
                this.damageType = reader.ReadDamageType();
                this.forceVector = reader.ReadVector3();
                this.pushAwayForce = reader.ReadSingle();
                this.upwardsForce = reader.ReadSingle();
                this.overlapInfoList.Clear();


                int i = 0;
                int num = (int)reader.ReadPackedUInt32();
                while (i < num)
                {
                    ExtendedOverlapAttack.OverlapInfo item = default(ExtendedOverlapAttack.OverlapInfo);
                    GameObject gameObject = reader.ReadHurtBoxReference().ResolveGameObject();
                    item.hurtBox = ((gameObject != null) ? gameObject.GetComponent<HurtBox>() : null);
                    item.hitPosition = reader.ReadVector3();
                    item.pushDirection = reader.ReadVector3();
                    this.overlapInfoList.Add(item);
                    i++;
                }
            }

            public string impactSound;

            public GameObject attacker;

            public GameObject inflictor;

            public float damage;

            public float upwardsForce;

            public bool isCrit;

            public ProcChainMask procChainMask;

            public float procCoefficient;

            public DamageColorIndex damageColorIndex;

            public DamageType damageType;

            public Vector3 forceVector;

            public float pushAwayForce;

            public readonly List<ExtendedOverlapAttack.OverlapInfo> overlapInfoList = new List<ExtendedOverlapAttack.OverlapInfo>();
        }
    }
}

