namespace AshenRoad.Boss.States
{
    /// <summary>Phase two (below the health threshold): faster swings, shorter recovery, a charge, fewer summons.</summary>
    public class BossPhaseTwoState : BossCombatState
    {
        public BossPhaseTwoState(BossController boss) : base(boss) { }

        protected override float WalkSpeed => boss.Data.phaseTwoWalkSpeed;
        protected override float SwingTelegraph => boss.Data.phaseTwoTelegraph;
        protected override float Recovery => boss.Data.phaseTwoRecovery;
        protected override float SummonInterval => boss.Data.phaseTwoSummonInterval;
        protected override bool CanCharge => true;
    }
}
