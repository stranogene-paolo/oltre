namespace Stranogene.Games.Oltre.Pilot
{
    /// <summary>
    /// PilotNameProfile
    /// Risultato runtime della generazione nome pilota.
    /// </summary>
    public struct PilotNameProfile
    {
        public string firstName;
        public string lastName;
        public string callsign;
        public string title;

        public string FullName => string.IsNullOrWhiteSpace(lastName) ? firstName : $"{firstName} {lastName}";
    }
}