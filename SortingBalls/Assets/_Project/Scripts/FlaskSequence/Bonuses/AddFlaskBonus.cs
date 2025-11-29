using Reflex.Attributes;

namespace _Project.Scripts.FlaskSequence.Bonuses
{
    public class AddFlaskBonus : Bonus
    {
        [Inject] private readonly LevelCreator LevelCreator;

        protected override void UseBonus()
        {
            LevelCreator.AddEmptyFlask();
        }

        protected override bool CanUseBonus()
        {
            return base.CanUseBonus() && LevelCreator.CanCreateFlask();
        }

        protected override string BuildSaveBonusesCountKey()
        {
            return $"Save_bonuses_count_to_bonus_'{typeof(AddFlaskBonus)}'";
        }
    }
}
