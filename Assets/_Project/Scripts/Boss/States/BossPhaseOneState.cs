namespace AshenRoad.Boss.States
{
    /// <summary>Phase one: slow telegraphed swings and Shambler summons.</summary>
    public class BossPhaseOneState : BossCombatState
    {
        public BossPhaseOneState(BossController boss) : base(boss) { }

        protected override float WalkSpeed => boss.Data.walkSpeed;
        protected override float SwingTelegraph => boss.Data.swingTelegraph;
        protected override float Recovery => boss.Data.swingRecovery;
        protected override float SummonInterval => boss.Data.summonInterval;
    }
}
