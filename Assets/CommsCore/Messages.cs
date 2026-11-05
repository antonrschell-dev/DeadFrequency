using System;
using System.Collections.Generic;

namespace DeadFrequency.CommsCore
{
    /// <summary>A relay link between a team and the Operator (Document 5, section 2).
    /// Backup is the low-bandwidth integrity channel: manipulation costs x2, delays capped.</summary>
    public enum LinkKind { TeamA, TeamB, Backup }

    public enum Priority { Routine, Distress }

    /// <summary>A transmission as composed by its sender. The server log of these,
    /// beside what was delivered, is the Transmission Reveal (the game's evidence chain).</summary>
    public sealed class Transmission
    {
        public string Id { get; }
        public string OriginId { get; }        // duplicates share the origin id (Doc 5, 5.3)
        public string SenderId { get; }
        public LinkKind Link { get; }
        public string Body { get; }
        public Priority Priority { get; }
        public double ComposedAtSeconds { get; }

        public Transmission(string id, string senderId, LinkKind link, string body,
                            Priority priority, double composedAtSeconds, string originId = null)
        {
            Id = id; SenderId = senderId; Link = link; Body = body;
            Priority = priority; ComposedAtSeconds = composedAtSeconds;
            OriginId = originId ?? id;
        }
    }

    /// <summary>What a recipient actually receives. Under Ruling #1 the inbox is
    /// hidden-source: no sender or routing is asserted here — SenderId is retained
    /// server-side for the Reveal only and must never be surfaced to clients.</summary>
    public sealed class DeliveredMessage
    {
        public Transmission Original { get; }
        public string DeliveredBody { get; internal set; }
        public Priority DeliveredPriority { get; internal set; }
        public double DisplayedComposedAtSeconds { get; internal set; }
        public double DeliveredAtSeconds { get; internal set; }
        public List<InterventionKind> Interventions { get; } = new List<InterventionKind>();
        public bool Touched => Interventions.Count > 0;

        public DeliveredMessage(Transmission original, double deliveredAtSeconds)
        {
            Original = original;
            DeliveredBody = original.Body;
            DeliveredPriority = original.Priority;
            DisplayedComposedAtSeconds = original.ComposedAtSeconds;
            DeliveredAtSeconds = deliveredAtSeconds;
        }
    }
}
