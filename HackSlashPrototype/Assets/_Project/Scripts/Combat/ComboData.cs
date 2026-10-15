using UnityEngine;

namespace HackSlash.Combat
{
    /// <summary>
    /// Árbol de combo estilo HIT:
    ///   L → L → L → L            (cadena ligera)
    ///   L → H, L → L → H, ...    (ramas pesadas según en qué golpe estés)
    ///   H desde neutral          (heavyStarter)
    /// Cambiar de arma = cambiar este asset.
    /// </summary>
    [CreateAssetMenu(menuName = "HackSlash/Combat/Combo Data", fileName = "CMB_New")]
    public sealed class ComboData : ScriptableObject
    {
        [Tooltip("Cadena de ataques ligeros en orden.")]
        [SerializeField] private AttackData[] lightChain = new AttackData[0];

        [Tooltip("Rama pesada por índice: [0] = L→H, [1] = L→L→H, ... Dejá null si no hay rama.")]
        [SerializeField] private AttackData[] heavyBranches = new AttackData[0];

        [Tooltip("Ataque pesado desde neutral (sin combo previo).")]
        [SerializeField] private AttackData heavyStarter;

        public int LightCount => lightChain.Length;
        public AttackData HeavyStarter => heavyStarter;

        public AttackData GetLight(int index) =>
            index >= 0 && index < lightChain.Length ? lightChain[index] : null;

        public AttackData GetHeavyBranch(int lightIndex) =>
            lightIndex >= 0 && lightIndex < heavyBranches.Length ? heavyBranches[lightIndex] : null;
    }
}
