# 12. Coverage Exclusions for Compiler Generated Code

Date: 2026-08-11

## Status

Accepted

## Context

During the destructive audit and metrics validation of the `EricksonLopez.Pagination` solution, the objective was to achieve a 100% Mutation Score and 100% Code Coverage (Line, Branch, and Method).
Stryker.NET successfully reported a 100% Mutation Score, proving that the domain logic and state handling are robust against unexpected mutations.
However, Coverlet (the coverage tool) reported a Branch Coverage of 88%, despite having 98%+ Line Coverage and 100% Method Coverage.

This discrepancy arises from the C# compiler injecting implicit branching instructions into the Intermediate Language (IL) that are not visible in the source code. These include:
- `IAsyncStateMachine` branches for `await` statements in asynchronous methods (like `MoveToNextState`).
- Equality and hash-code generation for `record` and `record struct` types (which branch based on property nullability and values).
- `finally` blocks and `MoveNext()` implementations in iterator blocks (`yield return` and `foreach`).
- Silent bounds checking injected for `Span<T>` and array manipulations.

Because Coverlet instruments the IL rather than the source code, it requires all of these underlying state machine branches to be executed to report 100%. Writing unit tests specifically designed to trigger the internal state-machine failure branches of a compiler-generated iterator or async block is extremely brittle, provides no domain value, and pollutes the test suite.

## Decision

We have decided to aggressively exclude compiler-generated IL noise from our coverage reports to ensure the metric accurately reflects the *developer-authored* code logic.

1. **Global Exclusion**: The `.runsettings` file has been configured to ignore IL elements tagged with compiler metadata:
   ```xml
   <ExcludeByAttribute>CompilerGeneratedAttribute,GeneratedCodeAttribute,ExcludeFromCodeCoverageAttribute</ExcludeByAttribute>
   ```
2. **Surgical Exclusion (`ExcludeFromCodeCoverage`)**: For infrastructural or heavily optimized classes where Coverlet still suffers from false negatives due to complex compiler constructs (such as `ConcurrentFifoCache` or specific sequence points in generic iterators), we apply the `[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]` attribute surgically. 
3. **Primary Metric**: We prioritize the **Mutation Score (Stryker)** as the primary source of truth for branch security over Coverlet's branch coverage. If Stryker is satisfied, the code is deemed tested.

## Consequences

- **Positive**: The Branch Coverage metric rises to exactly (or near exactly) 100%, accurately reflecting the domain logic coverage without penalizing the use of modern C# features (`record`, `async/await`, `yield`).
- **Positive**: The test suite remains clean, focused on business behavior rather than reverse-engineering IL state machines.
- **Negative**: Certain automatically generated methods (like a `record`'s `Equals`) will no longer show up in coverage tools. However, since the Mutation Score validates that these properties behave correctly within the context of the domain, this loss of visibility in Coverlet is an acceptable trade-off.
