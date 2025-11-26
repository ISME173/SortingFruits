using Reflex.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    }
}
