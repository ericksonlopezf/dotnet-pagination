# ADR-0027 — Tenant Isolation Responsibility Boundary

## Status
**Accepted** — August 2026

## Context

In multi-tenant SaaS applications, pagination cursors carry position information. The question arose whether cursors should embed tenant context to prevent cross-tenant cursor reuse.

## Problem

If Tenant A's cursor is submitted to a Tenant B's paginated endpoint, two scenarios arise:
1. **Authorization failure**: The application's authorization layer already validates tenant context and rejects unauthorized requests. The cursor position is irrelevant if the data is filtered by tenant.
2. **Information leakage**: If tenant isolation is not enforced at the application layer, cursor position values (e.g., `Id: 4521`) could be used to enumerate IDs across tenants.

## Decision

Tenant isolation is **the application's responsibility**, not the pagination library's. The library does not embed tenant context in cursors.

## Why

1. **Defense in depth**: Tenant isolation must exist at the data access layer (`WHERE TenantId = @tenantId`). A tenant-aware cursor is security theater if the query itself does not filter by tenant.
2. **Cursor complexity**: Adding tenant to the cursor requires: (a) consumer provides tenant ID at cursor generation, (b) library verifies tenant at decode, (c) adds a tenant field to the cursor payload.
3. **The HMAC already prevents tampering**: A cursor signed with HMAC cannot be modified by a malicious tenant without invalidating the signature. Cross-tenant reuse requires obtaining a valid cursor from another tenant's session, which is an application-layer authentication failure.
4. **Library should not know about tenancy**: Tenancy models vary (tenant per row, per schema, per database). Pagination should not be aware of any of these.

## Consequences

Consumers in multi-tenant applications should:
1. Ensure all paginated queries filter by tenant: `WHERE TenantId = @tenantId`
2. Use `HmacCursorEncoder` to prevent cursor tampering
3. Include tenant validation in their authorization layer, not cursor validation

## Documentation

A cookbook entry documents the recommended pattern for multi-tenant pagination, including warning about cross-tenant cursor reuse and recommending application-layer defense.

## Reconsideration Criteria

None. This is a permanent scope boundary. Adding tenant context to cursors does not add security; it only adds complexity.
