using System;

namespace Accord;

// A transition is complete only after both behaviour and layout succeed.
internal sealed class LiveUpdateState
{
    private uint observedJob;
    private ulong observedCharacter;
    private uint appliedJob;
    private ulong appliedCharacter;
    private long nextPoll;
    internal bool Applied => observedJob != 0 && observedJob == appliedJob && observedCharacter != 0 && observedCharacter == appliedCharacter;
    internal void Wake() => nextPoll = 0;
    internal void Invalidate() => appliedJob = 0;
    internal void Request() { Invalidate(); Wake(); }
    internal bool Poll(long now, uint job, ulong character)
    {
        if (job != observedJob || character != observedCharacter)
        {
            observedJob = job;
            observedCharacter = character;
            Request();
        }
        if (now < nextPoll) return false;
        nextPoll = now + (Applied ? 1000 : 250);
        return true;
    }
    internal void Apply(uint job, ulong character, Action operation)
    {
        operation();
        appliedJob = job;
        appliedCharacter = character;
    }
    // Engine notifications during our own apply must not turn a failed write into an every-frame retry.
    internal void DeferRetry(long now)
    {
        if (!Applied) nextPoll = now + 250;
    }
}
