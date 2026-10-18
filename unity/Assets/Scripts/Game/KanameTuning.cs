using Kaname.Sim;
using UnityEngine;

namespace Kaname.Game
{
    // The numbers of the design, as an asset the designer can tune in the Inspector (Assets/Settings/KanameTuning.asset). It only holds the
    // plain `Tuning` of the simulation, so there is one list of numbers, not two. The simulation reads the values live, so a change made in
    // the Inspector while the game runs takes effect on the next step.
    [CreateAssetMenu(fileName = "KanameTuning", menuName = "Kaname/Tuning")]
    public sealed class KanameTuning : ScriptableObject
    {
        public Tuning values = new Tuning();
    }
}
