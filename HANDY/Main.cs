using BepInEx;
using HANDY.HAND;
using HANDY.Weapon;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Skills;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using static HANDY.Helpers;

namespace HANDY
{

    [R2APISubmoduleDependency(new string[]
    {
        "LoadoutAPI",
        "PrefabAPI",
        "SurvivorAPI",
        "AssetPlus",
    })]
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.SushiDev.HAND", "HAN-D", "1.0.0")]
    public class HANDY : BaseUnityPlugin
    {
        public void Awake()
        {
            GameObject HAND = Resources.Load<GameObject>("Prefabs/CharacterBodies/HANDBody").InstantiateClone("HAND_CLONE", true);
            RegisterNewBody(HAND);

            var display = HAND.GetComponent<ModelLocator>().modelTransform.gameObject;

            CharacterBody characterBody = HAND.GetComponent<CharacterBody>();
            SkillLocator skillLocator = HAND.GetComponent<SkillLocator>();
            CharacterMotor characterMotor = HAND.GetComponent<CharacterMotor>();
            CharacterDirection characterDirection = HAND.GetComponent<CharacterDirection>();

            GlobalEventManager.onCharacterDeathGlobal += GlobalEventManagerOnOnCharacterDeath;

            display.AddComponent<HANDDisplayAnimation>();
            HAND.AddComponent<HANDOverclockController>();

            SetStateOnHurt hurtState = HAND.AddComponent<SetStateOnHurt>();

            SurvivorDef item = new SurvivorDef
            {
                //We're finding the body prefab here,
                bodyPrefab = HAND,
                //Description
                descriptionToken = "MANUAL NOT INCLUDED",
                //Display 
                displayPrefab = display,
                //Color on the select screen
                primaryColor = new Color(0.8039216f, 0.482352942f, 0.843137264f),
                //does literally nothing useful 
            };
            SurvivorAPI.AddSurvivor(item);

            R2API.AssetPlus.Languages.AddToken("HAND_CLONE_NAME_TOKEN", "HAN-D");
            R2API.AssetPlus.Languages.AddToken("HAND_PRIMARY_NAME", "HURT");
            R2API.AssetPlus.Languages.AddToken("HAND_PRIMARY_DESCRIPTION", "APPLY FORCE TO ALL COMBATANTS FOR <color=#E5C962>550% DAMAGE.</color>");
            R2API.AssetPlus.Languages.AddToken("HAND_UTILITY_NAME", "OVERCLOCK");
            R2API.AssetPlus.Languages.AddToken("HAND_UTILITY_DESCRIPTION", "INCREASE <color=#E5C962>ATTACK SPEED AND DAMAGE, AND SUMMON TEMPORARY DRONES ON COMBATANT DEATH. </color> ALL ATTACKS <color=#9CE562>HEAL 10% OF DAMAGE DONE</color>. <color=#95CDE5>INCREASE DURATION BY KILLING COMBATANTS.</color>");
            R2API.AssetPlus.Languages.AddToken("HAND_SPECIAL_NAME", "FORCED_REASSEMBLY");
            R2API.AssetPlus.Languages.AddToken("HAND_SPECIAL_DESCRIPTION", "APPLY GREAT FORCE TO THE GROUND, CAUSING AN EARTHQUAKE TO FORM. DEALS <color=#E5C962>600% DAMAGE</color> TO ENEMIES CAUGHT IN THE IMPACT. CAUSES <color=#E5C962>250% DAMAGE</color> TO ENEMIES CAUGHT IN THE EARTHQUAKE.");
            R2API.AssetPlus.Languages.AddToken("HAND_SPECIAL_ALT_NAME", "FORCED_REASSEMBLY");
            R2API.AssetPlus.Languages.AddToken("HAND_SPECIAL_ALT_DESCRIPTION", "APPLY GREAT FORCE TO THE GROUND, AND DEAL <color=#E5C962>600% DAMAGE</color> TO ENEMIES CAUGHT IN THE IMPACT.");

            characterMotor.mass = 250;
            characterDirection.turnSpeed = 320;

            characterBody.baseAcceleration = 80f;
            characterBody.baseJumpPower = 16;
            characterBody.baseArmor = 25;
            characterBody.preferredPodPrefab = Resources.Load<GameObject>("prefabs/networkedobjects/robocratepod");
            characterBody.baseDamage = 14;
            characterBody.levelDamage = 2.4f;
            characterBody.baseMaxHealth = 300;
            characterBody.levelMaxHealth = 80;
            characterBody.baseNameToken = "HAND_CLONE_NAME_TOKEN";
            characterBody.subtitleNameToken = "New Servos";

            characterBody.bodyFlags = CharacterBody.BodyFlags.ImmuneToExecutes;
            characterBody.bodyFlags = CharacterBody.BodyFlags.SprintAnyDirection;
            characterBody.bodyFlags = CharacterBody.BodyFlags.ResistantToAOE;
            characterBody.bodyFlags = CharacterBody.BodyFlags.Mechanical;

            hurtState.canBeFrozen = true;
            hurtState.canBeHitStunned = false;
            hurtState.canBeStunned = false;
            hurtState.hitThreshold = 5f;


            int i = 0;
            EntityStateMachine[] esmr = new EntityStateMachine[2];
            foreach (EntityStateMachine esm in HAND.GetComponentsInChildren<EntityStateMachine>())
            {
                switch (esm.customName)
                {
                    case "Body":
                        hurtState.targetStateMachine = esm;
                        break;
                    default:
                        if (i < 2)
                        {
                            esmr[i] = esm;
                            Debug.Log(esm.customName);
                        }
                        i++;
                        Debug.Log(i);
                        break;

                }
            }

            //GlobalEventManager.onCharacterDeathGlobal += GlobalEventManagerOnOnCharacterDeath;
            LoadoutAPI.AddSkill(typeof(HURT));
            LoadoutAPI.AddSkill(typeof(CHARGESLAM));
            LoadoutAPI.AddSkill(typeof(SLAM));
            LoadoutAPI.AddSkill(typeof(CHARGEBIGSLAM));
            LoadoutAPI.AddSkill(typeof(BIGSLAM));
            LoadoutAPI.AddSkill(typeof(OVERCLOCK));
            LoadoutAPI.AddSkill(typeof(POWERSAVER));



            SkillFamily primaryskillFamily = skillLocator.primary.skillFamily;
            SkillFamily secondaryskillFamily = skillLocator.secondary.skillFamily;
            SkillFamily utilityskillFamily = skillLocator.utility.skillFamily;
            SkillFamily specialskillFamily = skillLocator.special.skillFamily;
            SkillDef Primary = primaryskillFamily.variants[primaryskillFamily.defaultVariantIndex].skillDef;
            SkillDef Secondary = secondaryskillFamily.variants[secondaryskillFamily.defaultVariantIndex].skillDef;
            SkillDef Utility = utilityskillFamily.variants[utilityskillFamily.defaultVariantIndex].skillDef;
            SkillDef Special = specialskillFamily.variants[specialskillFamily.defaultVariantIndex].skillDef;

            var Special_Variants = specialskillFamily.variants;

            On.RoR2.CharacterModel.EnableItemDisplay += delegate (On.RoR2.CharacterModel.orig_EnableItemDisplay orig, CharacterModel self, ItemIndex itemIndex)
            {
                if (itemIndex != ItemIndex.Bear || self.name != "mdlHAND")
                {
                    orig(self, itemIndex);
                };
            };

            //Primary
            Primary.noSprint = false;
            Primary.canceledFromSprinting = false;
            Primary.baseRechargeInterval = 0;
            Primary.baseMaxStock = 1;
            Primary.rechargeStock = 1;
            Primary.shootDelay = 0.1f;
            Primary.beginSkillCooldownOnSkillEnd = false;
            Primary.isCombatSkill = true;
            Primary.mustKeyPress = false;
            Primary.requiredStock = 1;
            Primary.stockToConsume = 1;
            Primary.skillNameToken = "HAND_PRIMARY_NAME";
            Primary.skillDescriptionToken = "HAND_PRIMARY_DESCRIPTION";
            Primary.activationState = new EntityStates.SerializableEntityStateType(typeof(HURT));
            //Secondary
            Secondary.noSprint = false;
            Secondary.canceledFromSprinting = false;
            Secondary.baseRechargeInterval = 5;
            Secondary.baseMaxStock = 3;
            Secondary.rechargeStock = 1;
            Secondary.requiredStock = 1;
            Secondary.stockToConsume = 1;
            Secondary.isCombatSkill = true;
            Secondary.mustKeyPress = false;
            Secondary.isBullets = false;
            Secondary.shootDelay = 0.08f;
            Secondary.skillNameToken = ">:]";
            Secondary.skillDescriptionToken = ">:]";
            Secondary.activationState = new EntityStates.SerializableEntityStateType(typeof(POWERSAVER));

            //Utility 
            Utility.baseRechargeInterval = 15;
            Utility.noSprint = false;
            Utility.baseMaxStock = 1;
            Utility.isCombatSkill = false;
            Utility.canceledFromSprinting = false;
            Utility.rechargeStock = 1;
            Utility.requiredStock = 1;
            Utility.stockToConsume = 1;
            Utility.isBullets = false;
            Utility.shootDelay = 0.08f;
            Utility.skillNameToken = "HAND_UTILITY_NAME";
            Utility.skillDescriptionToken = "HAND_UTILITY_DESCRIPTION";
            Utility.activationState = new EntityStates.SerializableEntityStateType(typeof(OVERCLOCK));

            //Special
            Special.baseRechargeInterval = 8;
            Special.rechargeStock = 1;
            Special.noSprint = false;
            Special.beginSkillCooldownOnSkillEnd = true;
            Special.stockToConsume = 1;
            Special.requiredStock = 1;
            Special.baseMaxStock = 1;
            Special.canceledFromSprinting = false;
            Special.skillNameToken = "HAND_SPECIAL_NAME";
            Special.skillDescriptionToken = "HAND_SPECIAL_DESCRIPTION";
            Special.activationState = new EntityStates.SerializableEntityStateType(typeof(CHARGESLAM));

            var LoadoutSpecial = ScriptableObject.CreateInstance<SkillDef>();
            LoadoutSpecial.activationState = new EntityStates.SerializableEntityStateType(typeof(CHARGEBIGSLAM));
            LoadoutSpecial.baseMaxStock = 1;
            LoadoutSpecial.baseRechargeInterval = 12;
            LoadoutSpecial.beginSkillCooldownOnSkillEnd = true;
            LoadoutSpecial.canceledFromSprinting = true;    
            LoadoutSpecial.fullRestockOnAssign = false;
            LoadoutSpecial.isBullets = false;
            LoadoutSpecial.isCombatSkill = true;
            //LoadoutSpecial.mustKeyPress = true;
            LoadoutSpecial.noSprint = true;
            LoadoutSpecial.rechargeStock = 1;
            LoadoutSpecial.requiredStock = 1;
            LoadoutSpecial.shootDelay = 0.1f;
            LoadoutSpecial.activationStateMachineName = specialskillFamily.defaultSkillDef.activationStateMachineName;
            LoadoutSpecial.skillDescriptionToken = "HAND_SPECIAL_ALT_DESCRIPTION";
            LoadoutSpecial.skillNameToken = "HAND_SPECIAL_ALT_NAME";
            LoadoutSpecial.skillName = "HAND_SPECIAL_ALT_NAME";
            LoadoutSpecial.stockToConsume = 1;

            LoadoutAPI.AddSkillDef(LoadoutSpecial);
            SkillFamily.Variant LoadoutSpecialVariant = new SkillFamily.Variant
            {
                skillDef = LoadoutSpecial,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node("R", false)
            };
            Array.Resize<SkillFamily.Variant>(ref Special_Variants, Special_Variants.Length + 1);
            Special_Variants[Special_Variants.Length - 1] = LoadoutSpecialVariant;
            specialskillFamily.variants = Special_Variants;
        }
        /*private void GlobalEventManagerOnOnCharacterDeath(DamageReport damageReport)
        {
            if (damageReport.attackerBody.baseNameToken == "")
            {
            }
        }*/
        private void GlobalEventManagerOnOnCharacterDeath(DamageReport damageReport)
        {
            if (damageReport.victimBody.baseNameToken != "JELLYFISH_BODY_NAME")
            {
                if (damageReport.victimBody.baseNameToken != "DRONE_GUNNER_BODY_NAME")
                {
                    if (damageReport.victimBody.baseNameToken != "DRONE_HEALING_BODY_NAME")
                    {
                        if (damageReport.attackerBody.baseNameToken == "HAND_CLONE_NAME_TOKEN" && damageReport.attacker.GetComponent<HANDOverclockController>().overclockOn)
                        {
                            damageReport.attacker.GetComponent<HANDOverclockController>().AddDurationOnHit();
                            damageReport.attackerBody.healthComponent.Heal((damageReport.damageDealt / 15) * 100, default);
                            CharacterBody component = damageReport.attackerBody;
                            Debug.Log("characterbody component worked");
                            GameObject gameObject = MasterCatalog.FindMasterPrefab("Drone1Master");
                            Debug.Log("finding masterprefab worked");
                            GameObject bodyPrefab = BodyCatalog.FindBodyPrefab("Drone1Body");
                            Debug.Log("finding body worked");
                            var master = damageReport.attackerMaster;
                            Debug.Log("finding attackermaster worked");
                            GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject, component.transform.position, component.transform.rotation);
                            Debug.Log("Instantiate worked");
                            CharacterMaster component2 = gameObject2.GetComponent<CharacterMaster>();

                            component2.gameObject.AddComponent<MasterSuicideOnTimer>().lifeTimer = 120;

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
                                component5.isHealer = false;
                                component5.fullVision = true;
                            }
                            Inventory component6 = gameObject2.GetComponent<Inventory>();
                            Debug.Log("getting inv worked");
                            component6.CopyItemsFrom(master.inventory);
                            Debug.Log("copying worked");
                            NetworkServer.Spawn(gameObject2);
                            Debug.Log("network spawning worked");
                            CharacterBody body = component2.SpawnBody(bodyPrefab, component.transform.position + Vector3.up, component.transform.rotation);
                            Debug.Log("spawning body worked");
                        };
                    };
                };
            };
        }
       
        private class HANDDisplayAnimation : MonoBehaviour
        {

            internal void OnEnable()
            {
                if (gameObject.transform.parent.gameObject.name == "CharacterPad")
                {
                    Debug.Log("animation");
                    var animator = gameObject.GetComponent<Animator>();
                    Shooting(animator);

                }
                else
                {
                    Debug.Log("no animation");
                }
            }

            private void Shooting(Animator animator)
            {
                PlayAnimation("Gesture", "ChargeSlam", "ChargeSlam.playbackRate", EntityStates.HAND.Weapon.ChargeSlam.baseDuration, animator);

                var coroutine = Fire(animator);
                StartCoroutine(coroutine);
            }

            IEnumerator Fire(Animator animator)
            {
                yield return new WaitForSeconds(0.5f);

                PlayAnimation("Gesture", "Slam", "Slam.playbackRate", EntityStates.HAND.Weapon.Slam.baseDuration, animator);

                Destroy(this);
            }

            private void PlayAnimation(string layerName, string animationStateName, string playbackRateParam, float duration, Animator animator)
            {
                int layerIndex = animator.GetLayerIndex(layerName);
                animator.SetFloat(playbackRateParam, 1f);
                animator.PlayInFixedTime(animationStateName, layerIndex, 0f);
                animator.Update(0f);
                float length = animator.GetCurrentAnimatorStateInfo(layerIndex).length;
                animator.SetFloat(playbackRateParam, length / duration);
                Util.PlaySound("Play_MULT_shift_hit", this.gameObject);
            }
        }
    }
}