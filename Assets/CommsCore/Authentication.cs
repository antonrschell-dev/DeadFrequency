using System;
using System.Collections.Generic;

namespace DeadFrequency.CommsCore
{
    /// <summary>The players' counter-toolkit (Document 5, section 7): after Ruling #1 these
    /// are the game's ONLY trust mechanism, so they live in the core library, testable
    /// without the engine.</summary>
    public static class Authentication
    {
        /// <summary>Challenge/countersign (the fiction's "harbour" / "lantern" pair is one
        /// instance; matches configure their own).</summary>
        public static bool CheckCountersign(string challenge, string response,
                                            IReadOnlyDictionary<string, string> protocolBook)
        {
            if (challenge == null || response == null || protocolBook == null) return false;
            return protocolBook.TryGetValue(challenge.Trim().ToLowerInvariant(), out var expected)
                   && string.Equals(expected, response.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>The facility authentication word (fiction anchor: genuine TALLOW,
        /// the Listener's SABLE). One word, exact match, no partial credit.</summary>
        public static bool CheckAuthWord(string presented, string genuine)
            => presented != null && genuine != null
               && string.Equals(presented.Trim(), genuine.Trim(), StringComparison.OrdinalIgnoreCase);

        /// <summary>Landmark verification: a message that leans on a landmark is checked
        /// against the landmark's agreed true detail (fiction anchor: the kestrel mural's
        /// broken wing). Returns true only when the claimed detail matches the true one.</summary>
        public static bool CheckLandmark(string landmark, string claimedDetail,
                                         IReadOnlyDictionary<string, string> trueDetails)
        {
            if (landmark == null || claimedDetail == null || trueDetails == null) return false;
            return trueDetails.TryGetValue(landmark.Trim().ToLowerInvariant(), out var truth)
                   && string.Equals(truth, claimedDetail.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>The fairness constraint on authored deception (Audio Bible fairness law,
    /// generalized in Document 5): any single fabricated or modified delivery may contain
    /// AT MOST ONE wrong detail. Two or more makes the lie either trivially catchable or
    /// unfalsifiable noise — both are design bugs, so the core refuses to build it.</summary>
    public sealed class FabricationRequest
    {
        public string Body { get; }
        public int WrongDetailCount { get; }
        public bool Impersonation { get; }

        public FabricationRequest(string body, int wrongDetailCount, bool impersonation = false)
        {
            if (wrongDetailCount < 0) throw new ArgumentOutOfRangeException(nameof(wrongDetailCount));
            if (wrongDetailCount > 1)
                throw new ArgumentException(
                    "A fabrication may carry at most one wrong detail (fairness law).", nameof(wrongDetailCount));
            Body = body ?? throw new ArgumentNullException(nameof(body));
            WrongDetailCount = wrongDetailCount;
            Impersonation = impersonation;
        }
    }
}
