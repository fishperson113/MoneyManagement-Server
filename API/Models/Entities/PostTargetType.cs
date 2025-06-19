namespace API.Models.Entities
{
    public enum PostTargetType
    {
        Friends = 0,    // Default - current behavior (friends only)
        Private = 1,    // Only self
        Global = 2,     // All friends + all group members
        Groups = 3      // Specific groups only
    }
}
