# ADR-0038 — Adoption of Method_Scenario_Result (Osherove) Test Naming Convention

## Status
**Accepted** — August 2026

## Context
In high-reliability .NET libraries and enterprise architectures, test suites fulfill two fundamental roles:
1. **Automated Verification:** Guaranteeing that pagination, cursor encoding, SQL builder engines, and ORM adapters behave predictably across edge cases.
2. **Living Specifications (Executable Documentation):** Documenting the system's exact capabilities, failure modes, boundary constraints, and business rules in a human-readable form that outlives traditional documentation.

Standard .NET coding conventions enforced by Roslyn style analyzers (specifically rule **IDE1006: Naming Rule Violation**) mandate that all methods follow strict PascalCase without delimiters or special characters.

While strictly appropriate for production APIs (src/), applying monolithic PascalCase to test methods (e.g., KeysetPaginationWhenNullOrderingThrowsArgumentException) severely degrades readability and diagnostic velocity:
- **CI/CD Triage Friction:** When a test fails in headless CI pipelines (GitHub Actions, Azure DevOps, GitLab CI) or CLI runners (dotnet test), developers must instantly parse what failed, under what input state, and what the expected outcome was without opening the test implementation file.
- **Visual Congestion in Test Explorers:** Without clear structural delimiters, long descriptive names become illegible walls of text in IDE Test Explorers.
- **Ambiguity in Responsibility:** Unstructured naming leads to vague titles (TestPagination, CheckCursor, VerifyKeyset) that obscure the exact scenario under test.

## Decision
We officially adopt the **Method_Scenario_Result** pattern (the Osherove naming convention, based on Roy Osherove's *The Art of Unit Testing*) as the mandatory architectural standard for all unit, integration, and adapter test methods across the repository.

### 1. Tripartite Structural Taxonomy
Every test method name must consist of exactly three segments separated by single underscores (_):

\text{MethodUnderTest}\_\text{StateUnderTestOrScenario}\_\text{ExpectedBehaviorOrResult}

| Segment | Role | Description | Examples |
|---|---|---|---|
| **1. Method / Unit** | Target Under Test | The exact method, property, constructor, or operational unit being exercised. | ToPagedListAsync, EncodeCursor, BuildKeysetQuery, TryDecode |
| **2. Scenario** | State / Precondition | The specific input parameter, initial state, edge case, or context under test. | WhenEmptySource, WithTamperedHmacSignature, WhenNullOrdering, WithCompositeKeys |
| **3. Result** | Expected Outcome | The deterministic outcome, return value, state mutation, or exception thrown. | ReturnsEmptyPagedList, ThrowsInvalidCursorException, AppendsSeekPredicate, ReturnsFalse |

#### Canonical Examples:
- ToPagedListAsync_WhenEmptySource_ReturnsEmptyPagedList
- EncodeCursor_WithTamperedHmacSignature_ThrowsInvalidCursorException
- BuildKeysetQuery_WithCompositeKeys_AppendsSeekPredicate
- TryDecode_WhenPayloadCorrupted_ReturnsFalse

### 2. Local Suppression of Analyzer Rule IDE1006
To eliminate false-positive build failures under <TreatWarningsAsErrors>true</TreatWarningsAsErrors> without compromising production standards, we establish a **local scoping policy**:

1. **Scoped Bounded Suppression:** We disable IDE1006 **exclusively within the 	ests/ directory tree** using a local 	ests/.editorconfig.
2. **Production Invariance:** The root .editorconfig preserves strict PascalCase enforcement across all production assemblies (src/).

#### Configuration (	ests/.editorconfig):
```ini
[*.cs]
# Disables IDE1006 (Naming Rule Violation) specifically for test projects
# to permit the tripartite Method_Scenario_Result (Osherove) pattern,
# enabling tests to act as human-readable living specifications in CI/CD.
dotnet_diagnostic.IDE1006.severity = none
```

## Consequences
### Positive
- **Instant CI Diagnostic Velocity:** CI build logs and Test Explorers immediately communicate the failing unit, the failing context, and the violated expectation without opening source code.
- **Living Architecture Documentation:** Test suites serve as unambiguous, executable domain specifications.
- **Clean Separation of Concerns:** Production code strictly follows Microsoft BCL PascalCase standards, while test code follows domain-driven specification naming.
- **Zero Build Friction:** Eliminates compiler/analyzer warnings under zero-tolerance QA policies (TreatWarningsAsErrors).

### Negative
- **Deviates from BCL Production Conventions:** Test method names deliberately violate standard PascalCase method rules.
- **Requires Governance via EditorConfig:** Teams must maintain the isolated 	ests/.editorconfig across all testing directories.

## References
- Osherove, Roy. *The Art of Unit Testing: with examples in C#*. Manning Publications.
- Microsoft Learn: [IDE1006 - Naming Rule Violation](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide1006)
- Martin, Robert C. *Clean Code: A Handbook of Agile Software Craftsmanship*. Prentice Hall.
