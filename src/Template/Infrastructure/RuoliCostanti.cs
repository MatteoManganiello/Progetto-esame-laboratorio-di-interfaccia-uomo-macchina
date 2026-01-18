namespace Template.Infrastructure
{
    /// <summary>
    /// Costanti per i ruoli disponibili nel sistema
    /// </summary>
    public static class RuoliCostanti
    {
        public const string USER = "User";
        public const string ADMIN = "Admin";
        public const string SUPER_ADMIN = "SuperAdmin";

        public static readonly string[] TuttiRuoli = { USER, ADMIN, SUPER_ADMIN };
    }
}
