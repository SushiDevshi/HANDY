using EntityStates;
using UnityEngine;
using UnityEngine.Networking;

namespace HANDY.Weapon
{
    public class CHARGESLAM : BaseState
    {
        public float baseDuration = 1f;
        private float duration;
        private Animator modelAnimator;
        public override void OnEnter()
        {
            base.OnEnter();
            if (NetworkServer.active && base.isAuthority)
            {
                this.duration = this.baseDuration / this.attackSpeedStat;
                this.modelAnimator = base.GetModelAnimator();
                if (this.modelAnimator)
                {
                    base.PlayAnimation("Gesture", "ChargeSlam", "ChargeSlam.playbackRate", this.duration);
                }
                if (base.characterBody)
                {
                    base.characterBody.SetAimTimer(4f);
                }
            }
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (base.fixedAge >= this.duration && base.characterMotor.isGrounded && base.isAuthority)
            {
                this.outer.SetNextState(new SLAM());
                return;
            }
        }
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}
