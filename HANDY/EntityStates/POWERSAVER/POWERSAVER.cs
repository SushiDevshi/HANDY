using System;
using EntityStates;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace HANDY.Weapon
{
    public class POWERSAVER : BaseState
    {
        public static float baseDuration = .5f;
        private float duration;

        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = POWERSAVER.baseDuration / this.attackSpeedStat;
            CharacterBody component = base.characterBody;

            Debug.Log("characterbody component worked");
            GameObject gameObject = MasterCatalog.FindMasterPrefab("Drone2Master");
            Debug.Log("finding masterprefab worked");
            GameObject bodyPrefab = BodyCatalog.FindBodyPrefab("Drone2Body");
            Debug.Log("finding body worked");
            var master = component.master;
            Debug.Log("finding attackermaster worked");
            GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject, component.transform.position, component.transform.rotation);
            Debug.Log("Instantiate worked");
            CharacterMaster component2 = gameObject2.GetComponent<CharacterMaster>();
            component2.gameObject.AddComponent<MasterSuicideOnTimer>().lifeTimer = 15f;

            if (base.GetComponent<HANDOverclockController>().overclockOn)
            {
                component2.gameObject.AddComponent<MasterSuicideOnTimer>().lifeTimer = 25f;
            }

            component2.teamIndex = TeamComponent.GetObjectTeam(component.gameObject);
            AIOwnership component4 = gameObject2.GetComponent<AIOwnership>();
            BaseAI component5 = gameObject2.GetComponent<BaseAI>();
            if (component4)
            {
                component4.ownerMaster = master;
            }
            if (component5)
            {
                component5.leader.gameObject = master.gameObject;
                component5.isHealer = true;
                component5.fullVision = true;
            }
            Inventory component6 = gameObject2.GetComponent<Inventory>();
            Debug.Log("getting inv worked");
            component6.CopyItemsFrom(master.inventory);
            Debug.Log("copying worked");
            NetworkServer.Spawn(gameObject2);
            Debug.Log("network spawning worked");
            CharacterBody body = component2.SpawnBody(bodyPrefab, base.transform.position + Vector3.up, base.transform.rotation);
            Debug.Log("spawning body worked");
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
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
    }
}
