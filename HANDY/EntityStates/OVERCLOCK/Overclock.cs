using EntityStates;
using RoR2;
using RoR2.CharacterAI;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace HANDY.HAND
{   
    public class OVERCLOCK : BaseState
    {
        public float baseDuration = 0.25f;
        public override void OnEnter()
        {
            base.OnEnter();
            if (NetworkServer.active)
            {
                base.GetComponent<HANDOverclockController>().EnableOverclock();

                BlastAttack blastattack = new BlastAttack();
                blastattack.position = base.transform.position;
                blastattack.radius = 12;
                blastattack.damageType = DamageType.Stun1s;
                blastattack.baseForce = 0;
                blastattack.canHurtAttacker = false;
                blastattack.damageColorIndex = DamageColorIndex.Default;
                blastattack.falloffModel = BlastAttack.FalloffModel.None;
                blastattack.attacker = base.gameObject;
                blastattack.crit = RollCrit();

                blastattack.Fire();

            }
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (base.fixedAge > this.baseDuration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
                return;
            }
        }
    }
}
