using HANDY;
using HANDY.Weapon;
using RoR2;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace Servos
{
    public class ServosOverclockController : MonoBehaviour
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
        private Transform modelTransform;
        private TeamComponent teamComponent;
        private float duration;
        private float rechargingOnHitDuration;
        private SetStateOnHurt setStateOnHurt;
        private void Start()
        {
            this.characterBody = base.GetComponent<CharacterBody>();
            this.teamComponent = base.GetComponent<TeamComponent>();
            this.setStateOnHurt = base.GetComponent<SetStateOnHurt>();
            this.modelTransform = base.GetComponent<Transform>();
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
                this.overclockOn = true;
                this.characterBody.isChampion = true;
                this.setStateOnHurt.canBeFrozen = false;
                this.characterBody.baseDamage = this.characterBody.baseDamage + 10f;
                this.characterBody.baseAttackSpeed = this.characterBody.baseAttackSpeed + 5f;
                this.characterBody.baseMoveSpeed = this.characterBody.baseMoveSpeed + 10f;
                this.characterBody.AddTimedBuff(BuffIndex.HiddenInvincibility, 5f);
                this.duration = this.maxDuration;
                Util.PlaySound("Play_MULT_shift_start", base.gameObject);
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
                this.characterBody.baseDamage = this.characterBody.baseDamage - 5f;
                this.characterBody.baseAttackSpeed = this.characterBody.baseAttackSpeed - 2.5f;
                this.characterBody.baseMoveSpeed = this.characterBody.baseMoveSpeed - 5f;
            }
        }
    }
}