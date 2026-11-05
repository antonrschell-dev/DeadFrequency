using System;
using System.Collections.Generic;

namespace DeadFrequency.CommsCore
{
    public enum InterventionResult
    {
        Applied, UnknownMessage, AlreadyDelivered, InsufficientBudget,
        LockedByGraceWindow, BlockedBySuppressionLaw, InvalidOperation
    }

    /// <summary>The server-authoritative relay. Every transmission passes through it;
    /// the Listener acts only here, at budget cost, inside the fairness laws. The relay's
    /// TruthLog (originals beside deliveries) is what the Transmission Reveal renders —
    /// the Listener can touch deliveries, never the log (fairness spine, Document 5).</summary>
    public sealed class Relay
    {
        public const double GraceWindowSeconds = 180.0;
        public const double BackupDelayCapSeconds = 60.0;

        private sealed class Pending
        {
            public Transmission Tx;
            public DeliveredMessage Working;
            public double ReleaseAt;
            public bool Suppressed;
            public bool SuppressionImmune;   // set on the genuine message after a suppression on its link
        }

        private readonly Dictionary<string, Pending> _pending = new Dictionary<string, Pending>();
        private readonly List<DeliveredMessage> _delivered = new List<DeliveredMessage>();
        private readonly List<Transmission> _truthLog = new List<Transmission>();
        private readonly HashSet<LinkKind> _linkJustSuppressed = new HashSet<LinkKind>();

        public InterferenceBudget Budget { get; } = new InterferenceBudget();
        public double NowSeconds { get; private set; }
        public IReadOnlyList<DeliveredMessage> Delivered => _delivered;
        public IReadOnlyList<Transmission> TruthLog => _truthLog;

        public void Submit(Transmission tx)
        {
            if (_pending.ContainsKey(tx.Id)) throw new InvalidOperationException("duplicate transmission id");
            var p = new Pending
            {
                Tx = tx,
                Working = new DeliveredMessage(tx, NowSeconds),
                ReleaseAt = NowSeconds,
                SuppressionImmune = _linkJustSuppressed.Contains(tx.Link)
            };
            if (p.SuppressionImmune) _linkJustSuppressed.Remove(tx.Link);
            _pending[tx.Id] = p;
            _truthLog.Add(tx);
        }

        /// <summary>Apply one Listener intervention to a pending transmission.
        /// newBody is required for content edits and fabrication-style replacements.</summary>
        public InterventionResult Intervene(string transmissionId, InterventionKind kind,
                                            string newBody = null, bool impersonation = false,
                                            double delaySeconds = 30.0, Priority? newPriority = null,
                                            double? displayedComposedAt = null)
        {
            if (!_pending.TryGetValue(transmissionId, out var p)) return InterventionResult.UnknownMessage;
            if (NowSeconds < GraceWindowSeconds && InterventionCosts.LockedByGraceWindow(kind))
                return InterventionResult.LockedByGraceWindow;
            if (kind == InterventionKind.Suppression && p.SuppressionImmune)
                return InterventionResult.BlockedBySuppressionLaw;

            double cost = InterventionCosts.CostFor(kind, p.Tx.Link, impersonation);

            switch (kind)
            {
                case InterventionKind.Delay:
                    double d = p.Tx.Link == LinkKind.Backup ? Math.Min(delaySeconds, BackupDelayCapSeconds) : delaySeconds;
                    if (!Budget.TrySpend(cost)) return InterventionResult.InsufficientBudget;
                    p.ReleaseAt = Math.Max(p.ReleaseAt, NowSeconds + d);
                    break;
                case InterventionKind.Suppression:
                    if (!Budget.TrySpend(cost)) return InterventionResult.InsufficientBudget;
                    p.Suppressed = true;
                    _linkJustSuppressed.Add(p.Tx.Link);
                    break;
                case InterventionKind.Duplicate:
                    if (!Budget.TrySpend(cost)) return InterventionResult.InsufficientBudget;
                    p.Working.Interventions.Add(kind);
                    var copyTx = new Transmission(p.Tx.Id + "#dup", p.Tx.SenderId, p.Tx.Link,
                                                  p.Working.DeliveredBody, p.Tx.Priority, p.Tx.ComposedAtSeconds,
                                                  originId: p.Tx.OriginId);
                    var copy = new Pending { Tx = copyTx, Working = new DeliveredMessage(copyTx, NowSeconds), ReleaseAt = NowSeconds + Math.Max(60.0, delaySeconds) };
                    copy.Working.Interventions.Add(InterventionKind.Duplicate);
                    _pending[copyTx.Id] = copy;
                    return InterventionResult.Applied;
                case InterventionKind.WordSwap:
                case InterventionKind.NumberChange:
                case InterventionKind.DirectionSwap:
                case InterventionKind.NameSwap:
                case InterventionKind.LocationSwap:
                case InterventionKind.SentenceReplacement:
                case InterventionKind.Fabrication:
                    if (newBody == null) return InterventionResult.InvalidOperation;
                    if (!Budget.TrySpend(cost)) return InterventionResult.InsufficientBudget;
                    p.Working.DeliveredBody = newBody;
                    break;
                case InterventionKind.PriorityFlip:
                    if (!Budget.TrySpend(cost)) return InterventionResult.InsufficientBudget;
                    p.Working.DeliveredPriority = p.Working.DeliveredPriority == Priority.Routine ? Priority.Distress : Priority.Routine;
                    break;
                case InterventionKind.TimestampForgery:
                    if (displayedComposedAt == null) return InterventionResult.InvalidOperation;
                    if (!Budget.TrySpend(cost)) return InterventionResult.InsufficientBudget;
                    p.Working.DisplayedComposedAtSeconds = displayedComposedAt.Value;
                    break;
                default:
                    // SenderChange is metadata-only in the hidden-source model (no client effect);
                    // FalsePresence and CustodianManifestation do not target a transmission.
                    if (kind == InterventionKind.SenderChange)
                    {
                        if (!Budget.TrySpend(cost)) return InterventionResult.InsufficientBudget;
                        break;
                    }
                    return InterventionResult.InvalidOperation;
            }
            p.Working.Interventions.Add(kind);
            return InterventionResult.Applied;
        }

        /// <summary>Advance the match clock: budget regen, and delivery of everything
        /// whose time has come (and which was not suppressed).</summary>
        public void AdvanceTime(double seconds)
        {
            NowSeconds += seconds;
            Budget.AdvanceTime(seconds);
            var due = new List<Pending>();
            foreach (var p in _pending.Values)
                if (!p.Suppressed && p.ReleaseAt <= NowSeconds) due.Add(p);
            due.Sort((a, b) => a.ReleaseAt.CompareTo(b.ReleaseAt));
            foreach (var p in due)
            {
                p.Working.DeliveredAtSeconds = p.ReleaseAt;
                _delivered.Add(p.Working);
                _pending.Remove(p.Tx.Id);
            }
            // Suppressed messages leave the pending set silently at their release time;
            // they remain in the TruthLog, which is where the Reveal finds them.
            var gone = new List<string>();
            foreach (var kv in _pending)
                if (kv.Value.Suppressed && kv.Value.ReleaseAt <= NowSeconds) gone.Add(kv.Key);
            foreach (var id in gone) _pending.Remove(id);
        }
    }
}
