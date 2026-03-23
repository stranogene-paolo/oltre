using UnityEngine;

namespace Stranogene.Games.Oltre.Pilot
{
    /// <summary>
    /// PilotNameGenerator (procedural, no pools)
    /// Genera nomi "pronunciabili" tramite sillabe/pattern.
    /// Zero ScriptableObject, zero setup in Inspector.
    /// </summary>
    public static class PilotNameGenerator
    {
        // Set di sillabe semplici e “umane”.
        private static readonly string[] Onsets =
        {
            "b", "c", "d", "f", "g", "l", "m", "n", "p", "r", "s", "t", "v", "z",
            "br", "cr", "dr", "fr", "gr", "pr", "tr", "vr",
            "ch", "cl", "fl", "gl", "pl", "sl", "st", "sp"
        };

        private static readonly string[] Vowels =
        {
            "a", "e", "i", "o", "u",
            "ae", "ia", "io", "eo"
        };

        private static readonly string[] Codas =
        {
            "", "", "", // vuoti più frequenti → nomi più puliti
            "n", "l", "r", "s", "t", "m",
            "nd", "nt", "rt", "st", "rn", "rm"
        };

        // Per cognomi: un po’ più “robusti”
        private static readonly string[] SurnameCodas =
        {
            "", "",
            "i", "o", "a",
            "ni", "ri", "ti", "li",
            "son", "sen", "man",
            "etti", "elli", "aro", "one"
        };

        // Callsign (opzionale)
        private static readonly string[] CallsignPrefixes =
        {
            "K", "AX", "NX", "RV", "ST", "OX", "V", "Z", "IR", "Q"
        };

        private static readonly string[] CallsignWords =
        {
            "NOVA", "ECHO", "ORION", "VESPA", "ATLAS", "DRIFT", "LUNA", "ARGO", "EMBER", "MOSS"
        };

        /// <summary>
        /// Genera nome pilota. Se vuoi determinismo per run, passa un seed.
        /// </summary>
        public static PilotNameProfile Generate(int seed)
        {
            var prev = Random.state;
            Random.InitState(seed);

            var first = MakeName(isSurname: false);
            var last = MakeName(isSurname: true);

            // 60% callsign alfanumerico, 40% parola
            var callsign = Random.value < 0.6f ? MakeAlphaNumericCallsign() : MakeWordCallsign();

            // Title per ora vuoto (lo leghiamo ai trait più avanti)
            var title = "";

            var profile = new PilotNameProfile
            {
                firstName = first,
                lastName = last,
                callsign = callsign,
                title = title
            };

            Random.state = prev;
            return profile;
        }

        public static string BuildDisplayName(PilotNameProfile p, bool includeCallsign = true, bool includeTitle = true)
        {
            var display = p.FullName;

            if (includeCallsign && !string.IsNullOrWhiteSpace(p.callsign))
                display += $" \"{p.callsign}\"";

            if (includeTitle && !string.IsNullOrWhiteSpace(p.title))
                display += $" — {p.title}";

            return display;
        }

        private static string MakeName(bool isSurname)
        {
            // 2–3 sillabe per first name, 2–4 per surname
            var syllables = isSurname ? Random.Range(2, 5) : Random.Range(2, 4);

            var s = "";

            for (var i = 0; i < syllables; i++)
            {
                var onset = Pick(Onsets);
                var vowel = Pick(Vowels);

                // coda più frequente alla fine
                var coda = (i == syllables - 1)
                    ? (isSurname ? Pick(SurnameCodas) : Pick(Codas))
                    : "";

                // evita doppie consonanti brutte
                if (s.Length > 0 && onset.Length > 0)
                {
                    var lastChar = s[s.Length - 1];
                    if (lastChar == onset[0]) onset = onset.Substring(1);
                }

                s += onset + vowel + coda;
            }

            // Capitalize
            if (s.Length == 0) return isSurname ? "Unknown" : "Pilot";
            return char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        private static string MakeAlphaNumericCallsign()
        {
            var p = Pick(CallsignPrefixes);
            var a = Random.Range(0, 100);
            // es: AX-07, K-42
            return $"{p}-{a:00}";
        }

        private static string MakeWordCallsign()
        {
            var w = Pick(CallsignWords);
            // 50% aggiunge suffisso numerico
            if (Random.value < 0.5f)
                w += "-" + Random.Range(1, 10);
            return w;
        }

        private static string Pick(string[] arr)
        {
            if (arr == null || arr.Length == 0) return "";
            return arr[Random.Range(0, arr.Length)];
        }
    }
}