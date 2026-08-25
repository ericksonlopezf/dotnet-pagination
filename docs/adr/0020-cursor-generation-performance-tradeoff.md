# ADR 0020: Cursor Generation Performance Trade-off

## Status

Accepted

## Date

2026-08-11

## Context
During benchmark comparisons against `MR.EntityFrameworkCore.KeysetPagination`, we noticed a persistent overhead of ~0.9ms per request for `EricksonLopez.Pagination` (2.26ms vs 1.31ms). 

Analysis confirmed that the EF Core query compilation, parameterization, and database execution times were identical. The 0.9ms difference is strictly localized to C# CPU overhead during the execution of `ToCursorPagedListAsync()`. 

Unlike competitors that only return the raw data and force the developer to manually extract reference objects, our library takes an "out-of-the-box" secure approach. The 0.9ms overhead is explicitly spent on:
1. **Dynamic Deserialization:** Parsing incoming Base64 cursors and strongly coercing string representations back into `Type` using `ValueCoercer` (to avoid generic reflection penalties).
2. **Dynamic Generation:** Extracting values from the first and last returned entities using compiled Expression-Tree delegates (cached in `PaginationExpressionCache`) to generate the `S|v2|` keyset token.
3. **Cryptographic Security:** Hashing the keyset schema fingerprint via FNV-1a and applying an HMAC SHA-256 signature to the payload, followed by UTF8 Base64 encoding.

## Decision
We will consciously accept the ~0.9ms CPU overhead incurred by automatic cursor generation, parsing, and HMAC encryption. We will **not** strip out security or ergonomics to win synthetic benchmarks.

## Consequences
- **Positive:** Developers receive enterprise-grade, tamper-proof keyset pagination with zero configuration.
- **Positive:** Prevents widespread security vulnerabilities such as ID-enumeration and cursor manipulation.
- **Negative:** `EricksonLopez.Pagination` will technically rank slightly behind bare-bones ORM wrappers in synthetic micro-benchmarks that do not account for end-to-end cursor string generation.

## Justification
A pagination library that does not generate or parse the cursor string is only doing half the job. The developer would inevitably have to write the string-concatenation, parsing, and encryption logic themselves, re-introducing the 0.9ms overhead into their application layer (often doing it less efficiently and less securely). Absorbing this overhead at the library level ensures maximum security and developer productivity, which aligns perfectly with our core philosophy.
