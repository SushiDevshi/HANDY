using HANDY;
using HANDY.Weapon;
using RoR2;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace HANDY
{
    public class HANDOverclockController : MonoBehaviour
    {
        //public CharacterModel.RendererInfo[] rendererInfos;
        //Texture2D golden;
        //Texture2D blue;
        public bool rechargingOnHit = false;
        public bool overclockOn = false;
        public float stunChance = 30f;
        public float attackSpeedBonus = 0.3f;
        public float maxDuration = 3;
        public float durationOnHit = 3f;
        public float healPercentOnHit = 0.06f;
        public float overclockTargetArmor = 30f;
        private CharacterBody characterBody;
        private TeamComponent teamComponent;
        private float duration;
        private float rechargingOnHitDuration;
        private SetStateOnHurt setStateOnHurt;
        private void Start()
        {
            this.characterBody = base.GetComponent<CharacterBody>();
            this.teamComponent = base.GetComponent<TeamComponent>();
            this.setStateOnHurt = base.GetComponent<SetStateOnHurt>();
            //this.rendererInfos = base.GetComponent<CharacterModel>().baseRendererInfos;
            this.overclockOn = false;
        }
        private void Update()
        {
            if (this.rechargingOnHit)
            {
                this.rechargingOnHitDuration -= Time.deltaTime;
                if (this.rechargingOnHitDuration < 0f)
                {
                    this.rechargingOnHit = false;
                }
            }
            if (this.duration > 0f)
            {
                this.duration -= Time.deltaTime;
            }
            if (this.duration < 0f)
            {
                this.DisableOverclock();
            }
        }
        public void EnableOverclock()
        {
            if (!this.overclockOn)
            {
                //this.golden = new Texture2D(1, 1);
                //this.rendererInfos = base.GetComponent
                //Material defaultmaterial = rendererInfos[0].defaultMaterial;
                //this.blue = (defaultmaterial.GetTexture("matHAND") as Texture2D);
                //defaultmaterial.mainTexture = (this.golden as Texture);
                this.overclockOn = true;
                this.characterBody.isChampion = true;
                this.setStateOnHurt.canBeFrozen = false;
                this.characterBody.baseDamage = this.characterBody.baseDamage + 1.5f;
                this.characterBody.baseAttackSpeed = this.characterBody.baseAttackSpeed + 1.5f;
                this.characterBody.baseMoveSpeed = this.characterBody.baseMoveSpeed + 1.5f;
                this.characterBody.AddTimedBuff(BuffIndex.HiddenInvincibility, 1.5f);
                this.duration = this.maxDuration;
                Util.PlaySound("Play_MULT_shift_start", base.gameObject);

                if (NetworkServer.active)
                {
                    this.characterBody.RecalculateStats();
                }
                else
                {
                    if (ClientScene.ready)
                    {
                        this.WriteOverclockInfo(ExtendedOverlapAttack.write);
                    }
                }

            }

        }

        public void DisableOverclock()
        {
            if (this.overclockOn)
            {
                this.overclockOn = false;
                this.rechargingOnHit = false;
                Util.PlaySound("Play_MULT_shift_end", base.gameObject);
                this.setStateOnHurt.canBeFrozen = true;
                this.characterBody.isChampion = false;
                this.characterBody.baseDamage = this.characterBody.baseDamage - 1.5f;
                this.characterBody.baseAttackSpeed = this.characterBody.baseAttackSpeed - 1.5f;
                this.characterBody.baseMoveSpeed = this.characterBody.baseMoveSpeed - 1.5f;
                if (NetworkServer.active)
                {
                    this.characterBody.RecalculateStats();
                }
                else
                {
                    if (ClientScene.ready)
                    {
                        this.WriteOverclockInfo(ExtendedOverlapAttack.write);
                    }
                }
            }
        }
        public void AddDurationOnHit()
        {
            if (NetworkServer.active)
            {
                this.rechargingOnHit = true;
                this.rechargingOnHitDuration = 0.5f * (HURT.baseDuration / (this.characterBody.attackSpeed + this.attackSpeedBonus));
                this.duration += this.durationOnHit;
                bool flag = this.duration > this.maxDuration;
                if (this.duration > this.maxDuration)
                {
                    this.duration = this.maxDuration;
                }
            }
        }

        public void WriteOverclockInfo(NetworkWriter writer)
        {
            ExtendedOverlapAttack.write.StartMessage(77);
            writer.Write(this.rechargingOnHit);
            writer.Write(this.overclockOn);
            writer.Write(this.stunChance);
            writer.Write(this.attackSpeedBonus);
            writer.Write(this.maxDuration);
            writer.Write(this.healPercentOnHit);
            writer.Write(this.overclockTargetArmor);
            writer.Write(this.characterBody);
            writer.Write(this.duration);
            writer.Write(this.rechargingOnHitDuration);
            writer.Write(this.setStateOnHurt);
            writer.Write(this.rechargingOnHit);
            writer.Write(this.characterBody.baseAttackSpeed);
            writer.Write(this.characterBody.baseDamage);
            ExtendedOverlapAttack.write.FinishMessage();
            ClientScene.readyConnection.SendWriter(ExtendedOverlapAttack.write, QosChannelIndex.defaultReliable.intVal);
        }
    }
}
        /*public class OverclockMessage : MessageBase
        {
            public override void Serialize(NetworkWriter writer)
            {
                base.Serialize(writer);
                writer.Write(this.rechargingOnHit);
                writer.Write(this.overclockOn);
                writer.Write(this.stunChance);
                writer.Write(this.attackSpeedBonus);
                writer.Write(this.maxDuration);
                writer.Write(this.healPercentOnHit);
                writer.Write(this.overclockTargetArmor);
                writer.Write(this.characterBody);
                writer.Write(this.duration);
                writer.Write(this.rechargingOnHitDuration);
                writer.Write(this.setStateOnHurt);
                writer.Write(this.rechargingOnHit);
                writer.Write(this.characterBody.baseAttackSpeed);
                writer.Write(this.characterBody.baseDamage);
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
        }
    }
}*/