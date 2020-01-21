using EntityStates;
using UnityEngine;

namespace HANDY.Weapon
{
    public class CHARGEBIGSLAM : BaseState
    {
        public float baseDuration = 1.2f;
        private float duration;
        private Animator modelAnimator;
        public override void OnEnter()
        {
            base.OnEnter();
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
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (base.fixedAge >= this.duration && base.characterMotor.isGrounded && base.isAuthority)
            {
                this.outer.SetNextState(new BIGSLAM());
                return;
            }
        }
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}
