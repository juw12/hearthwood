namespace Hearthwood.Core
{
    /// <summary>
    /// Origin of damage. Used by <c>HealthComponent</c> to apply damage type modifiers and by
    /// the audio system to pick the right hit reaction.
    /// </summary>
    public enum DamageSource
    {
        Generic = 0,
        EnemyMelee = 1,
        EnemyRanged = 2,
        PlayerMelee = 3,
        Fall = 4,
        Environmental = 5,
        Fire = 6,
        Magic = 7,
    }
}
