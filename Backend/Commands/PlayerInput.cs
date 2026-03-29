namespace Backend.Commands
{
    internal static class PlayerInput
    {
        internal static bool TryParse(string player, out string ign, out string tagline)
        {
            var parts = player.Split('#', 2);
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            {
                ign = string.Empty;
                tagline = string.Empty;
                return false;
            }

            ign = parts[0].Trim();
            tagline = parts[1].Trim();
            return true;
        }
    }
}
