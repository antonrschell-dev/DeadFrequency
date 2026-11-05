using System;
using System.Collections.Generic;

namespace DeadFrequency.CommsCore
{
    public enum IntegrityReading { Clean, Flagged }

    /// <summary>The inbox's signal-integrity indicator (Ruling #1 model). It is genuinely
    /// informative and genuinely fallible: it flags most touched messages, misses some,
    /// and occasionally flags a clean one. It is a Listener surface — a hint with an
    /// agenda, never a verdict. Deterministic under a seed so matches can be replayed
    /// and tests can be exact.</summary>
    public sealed class IntegrityIndicator
    {
        private readonly Random _rng;
        private readonly double _missRate;    // touched message reads Clean
        private readonly double _falseRate;   // clean message reads Flagged

        public IntegrityIndicator(int seed, double missRate = 0.25, double falseRate = 0.08)
        {
            _rng = new Random(seed);
            _missRate = missRate; _falseRate = falseRate;
        }

        public IntegrityReading Read(DeliveredMessage message)
        {
            double roll = _rng.NextDouble();
            if (message.Touched) return roll < _missRate ? IntegrityReading.Clean : IntegrityReading.Flagged;
            return roll < _falseRate ? IntegrityReading.Flagged : IntegrityReading.Clean;
        }
    }

    public enum ExertionKind { Scream, RunningFeet, Impact, MachineryStrain }

    /// <summary>What a team perceives from Ambient Bleed (Ruling #4). Deliberately has
    /// NO source zone and NO direction: bleed says something is happening, never where.
    /// The type system is the design enforcement — there is no field to leak it through.</summary>
    public sealed class BleedPercept
    {
        public ExertionKind Kind { get; }
        public bool Fabricated { get; }   // server truth for the Reveal; clients never see this flag in play
        internal BleedPercept(ExertionKind kind, bool fabricated) { Kind = kind; Fabricated = fabricated; }
    }

    /// <summary>Ambient Bleed + False Presence (Ruling #4), with the voice-sampling gate
    /// (the Listener must have intercepted a voice before it can call in it) and the
    /// fragments-only law (Ruling #5: the lure vocabulary has no sentence — by type).</summary>
    public sealed class AmbientBleed
    {
        private readonly Dictionary<string, HashSet<string>> _adjacent = new Dictionary<string, HashSet<string>>();
        private readonly Dictionary<string, string> _teamZone = new Dictionary<string, string>();
        private readonly HashSet<string> _sampledVoices = new HashSet<string>();

        public void SetAdjacency(string zoneA, string zoneB)
        {
            if (!_adjacent.TryGetValue(zoneA, out var a)) _adjacent[zoneA] = a = new HashSet<string>();
            if (!_adjacent.TryGetValue(zoneB, out var b)) _adjacent[zoneB] = b = new HashSet<string>();
            a.Add(zoneB); b.Add(zoneA);
        }

        public void SetTeamZone(string teamId, string zone) => _teamZone[teamId] = zone;

        /// <summary>The Listener hears relay traffic for free (Document 5): when a player's
        /// voice crosses the relay, it becomes sampleable for False Presence name-calls.</summary>
        public void NoteInterceptedVoice(string playerId) => _sampledVoices.Add(playerId);
        public bool HasSampleOf(string playerId) => _sampledVoices.Contains(playerId);

        /// <summary>Real exertion bleeds to the other team only when the teams are in
        /// adjacent zones. Returns the percept for the listening team, or null.</summary>
        public BleedPercept Exertion(string actingTeamId, string listeningTeamId, ExertionKind kind)
        {
            if (!_teamZone.TryGetValue(actingTeamId, out var za)) return null;
            if (!_teamZone.TryGetValue(listeningTeamId, out var zl)) return null;
            if (!_adjacent.TryGetValue(za, out var near) || !near.Contains(zl)) return null;
            return new BleedPercept(kind, fabricated: false);
        }

        /// <summary>A fabricated lure. Generic screams are always available; a NAME-CALL in
        /// a specific player's voice additionally requires a sample of that voice.
        /// There is deliberately no overload that takes sentence text (Ruling #5).</summary>
        public BleedPercept FalsePresence(ExertionKind kind, string nameCallVoiceOfPlayerId = null)
        {
            if (nameCallVoiceOfPlayerId != null && !HasSampleOf(nameCallVoiceOfPlayerId)) return null;
            return new BleedPercept(kind, fabricated: true);
        }
    }
}
