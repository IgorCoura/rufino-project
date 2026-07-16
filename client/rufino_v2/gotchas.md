# Gotchas

Patterns turned into rules after a correction. Review at session start.

## Invariants belong in the constructor, not in the caller

**What happened.** A rule (`ExpirationPolicy`) could not exist with a zeroed
duration. I proposed guarding the caller that composed it. I was asked: "não
teria como deixar dentro da própria policy?" — and that was right.

**Why the caller guard was wrong.** A guard protects *one path*. The bug being
fixed had exactly that shape: a guard on the derivation path left the
explicit-construction path open, which was the path the app was about to use. A
caller guard would have repeated the same mistake somewhere else.

**The rule.** When a value is invalid *by definition*, reject it in the
constructor. Then there is no path to protect — the invalid state cannot be
built, including through deserialisation, which no caller guard would have
covered.

**How to apply.** Before adding `if (x <= 0) skip` in a caller, ask whether the
type should refuse `x <= 0` at all. If yes, the check goes in the type. A caller
may still *skip* (rather than throw) when legacy data legitimately holds the
invalid value — those two are complementary, not alternatives.

## Absence and zero are different values

**The rule.** When a model reads presence as meaning, never collapse `null` into
`0` (or `''`). `(value ?? 0)` in a repository silently turned "no expiration
rule" into "expiration rule of zero days" all the way to the server.

**How to apply.** If the API distinguishes absent from zero, the DTO field must
be nullable end to end. Assert on the **raw request body** in tests — a test
against the returned `Result` cannot see the difference.
