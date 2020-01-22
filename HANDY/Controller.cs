using HANDY.Weapon;
using RoR2;
using UnityEngine;

// Token: 0x02000002 RID: 2
public class HANDOverclockController : MonoBehaviour
{

    public bool rechargingOnHit = false;
    public bool overclockOn = false;
    public float stunChance = 30f;
    public float attackSpeedBonus = 0.3f;
    public float maxDuration = 3f;
    public float durationOnHit = 3f;
    public float healPercentOnHit = 0.06f;
    public float overclockTargetArmor = 30f;
    private CharacterBody characterBody;
    private TeamComponent teamComponent;
    private float duration;
    private float rechargingOnHitDuration;
    public bool useGroundPound = false;
    public bool executeGroundPound = false;
    private CharacterMotor characterMotor;
    private SetStateOnHurt setStateOnHurt;
    private void Start()
    {
        this.characterBody = base.GetComponent<CharacterBody>();
        this.characterMotor = base.GetComponent<CharacterMotor>();
        this.teamComponent = base.GetComponent<TeamComponent>();
        this.setStateOnHurt = base.GetComponent<SetStateOnHurt>();
        this.overclockOn = false;
    }

    // Token: 0x06000002 RID: 2 RVA: 0x00002080 File Offset: 0x00000280
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
            this.characterBody.baseDamage = this.characterBody.baseDamage + 1;
            this.characterBody.baseMoveSpeed = this.characterBody.baseMoveSpeed + 2;
            this.characterBody.baseArmor = this.characterBody.baseArmor + 3;
            this.characterBody.baseCrit = this.characterBody.baseCrit + 2;
            Util.PlaySound("Play_MULT_shift_start", base.gameObject);
        }
        this.characterBody.AddTimedBuff(BuffIndex.HiddenInvincibility, 1.25f);
        this.duration = this.maxDuration;
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
            this.characterBody.baseDamage = this.characterBody.baseDamage - 1;
            this.characterBody.baseMoveSpeed = this.characterBody.baseMoveSpeed - 2;
            this.characterBody.baseArmor = this.characterBody.baseArmor - 3;
            this.characterBody.baseCrit = this.characterBody.baseCrit - 2;
        }
    }
    public void AddDurationOnHit()
    {
        this.rechargingOnHit = true;
        this.rechargingOnHitDuration = 0.5f * (HURT.baseDuration / (this.characterBody.attackSpeed + this.attackSpeedBonus));
        this.duration += this.durationOnHit;
        bool flag = this.duration > this.maxDuration;
        if (flag)
        {
            this.duration = this.maxDuration;
        }
    }
}

