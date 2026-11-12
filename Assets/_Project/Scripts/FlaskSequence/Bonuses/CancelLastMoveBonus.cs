using Reflex.Attributes;

namespace _Project.Scripts.FlaskSequence.Bonuses
{
    public class CancelLastMoveBonus : Bonus
    {
        private FlaskItemsMover _flaskItemsMover;

        protected override void UseBonus()
        {
            _flaskItemsMover.TryCancelLastMove();
        }

        protected override bool CanUseBonus()
        {
            return base.CanUseBonus() && _flaskItemsMover.CanCancelLastMove();
        }

        [Inject]
        private void Initialize(FlaskItemsMover flaskItemsMover)
        {
            _flaskItemsMover = flaskItemsMover;
        }

        protected override string BuildSaveBonusesCountKey()
        {
            return $"Save_bonuses_count_to_bonus_'{typeof(CancelLastMoveBonus)}'";
        }
    }
}
