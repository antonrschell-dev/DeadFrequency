using System;

namespace DeadFrequency.CommsCore
{
    /// <summary>The Listener's economy, canonical numbers from Document 5, section 3.4:
    /// pool 100, start 60, regen 5/minute, +20 at each phase surge. The economy is the
    /// fairness spine: it buys constant pressure, never constant control.</summary>
    public sealed class InterferenceBudget
    {
        public const double PoolCapacity = 100.0;
        public const double StartingBalance = 60.0;
        public const double RegenPerMinute = 5.0;
        public const double PhaseSurgeAmount = 20.0;

        public double Balance { get; private set; } = StartingBalance;

        public void AdvanceTime(double seconds)
        {
            if (seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            Balance = Math.Min(PoolCapacity, Balance + RegenPerMinute * (seconds / 60.0));
        }

        /// <summary>Granted when the match advances Search to Contact and Contact to Endgame.</summary>
        public void PhaseSurge() => Balance = Math.Min(PoolCapacity, Balance + PhaseSurgeAmount);

        public bool CanAfford(double cost) => Balance >= cost;

        public bool TrySpend(double cost)
        {
            if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
            if (!CanAfford(cost)) return false;
            Balance -= cost;
            return true;
        }
    }

    /// <summary>Intervention kinds with canonical IB costs (Document 5, section 3.3 table and
    /// section 3.4 economy reading). Delay/Duplicate/Suppression costs are economy-derived
    /// (the document prices suppression at roughly four minutes of banked regen; fabrication
    /// at 28 with the 15 impersonation surcharge stated verbatim). FalsePresence is priced
    /// from Ruling #4's design addendum (a name-call costs more than a generic scream: the
    /// scream is the 9 IB base here; name-call = base + impersonation-style surcharge is
    /// handled in AmbientBleed). ChannelDemotion from the original table is REMOVED:
    /// Ruling #1 (hidden-source inbox) leaves no channel label to demote.</summary>
    public enum InterventionKind
    {
        PriorityFlip,       // 5
        TimestampForgery,   // 6
        Delay,              // 6
        WordSwap,           // 8
        FalsePresence,      // 9 (generic scream / exertion lure)
        NumberChange,       // 10
        Duplicate,          // 10
        DirectionSwap,      // 12
        NameSwap,           // 12
        LocationSwap,       // 12
        SenderChange,       // 12 (same-link terminal only)
        SentenceReplacement,// 18
        Suppression,        // 22
        Fabrication,        // 28
        CustodianManifestation // 30 (Ruling #11; paper-prototype price, digital cost TBD by balance pass)
    }

    public static class InterventionCosts
    {
        public const double ImpersonationSurcharge = 15.0;

        public static double BaseCost(InterventionKind kind)
        {
            switch (kind)
            {
                case InterventionKind.PriorityFlip: return 5;
                case InterventionKind.TimestampForgery: return 6;
                case InterventionKind.Delay: return 6;
                case InterventionKind.WordSwap: return 8;
                case InterventionKind.FalsePresence: return 9;
                case InterventionKind.NumberChange: return 10;
                case InterventionKind.Duplicate: return 10;
                case InterventionKind.DirectionSwap: return 12;
                case InterventionKind.NameSwap: return 12;
                case InterventionKind.LocationSwap: return 12;
                case InterventionKind.SenderChange: return 12;
                case InterventionKind.SentenceReplacement: return 18;
                case InterventionKind.Suppression: return 22;
                case InterventionKind.Fabrication: return 28;
                case InterventionKind.CustodianManifestation: return 30;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        /// <summary>Backup-link rule (Document 5, section 2): manipulations against the
        /// Backup Channel cost double. Fabrication may additionally carry the
        /// impersonation surcharge when it wears a known sender's style.</summary>
        public static double CostFor(InterventionKind kind, LinkKind link, bool impersonation = false)
        {
            double cost = BaseCost(kind);
            if (impersonation && kind == InterventionKind.Fabrication) cost += ImpersonationSurcharge;
            if (link == LinkKind.Backup) cost *= 2.0;
            return cost;
        }

        /// <summary>The Grace Window (Document 5): the first 3 minutes of a match,
        /// fabrication and suppression are locked. Everything else is legal from second zero.</summary>
        public static bool LockedByGraceWindow(InterventionKind kind)
            => kind == InterventionKind.Fabrication || kind == InterventionKind.Suppression;
    }
}
