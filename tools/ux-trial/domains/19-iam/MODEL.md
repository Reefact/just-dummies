# IAM — domain model

Namespace `Identity.Domain` (`Domain/`). OAuth clients with registered redirect URIs and allowed scopes, access tokens issued under a policy, and role assignments.

## Value objects
- `ClientId(Guid value)` — not `Guid.Empty` (`ArgumentException`). Equality by value; `ToString()` = GUID in `D` format.
- `Scope(string value)` — not null/blank (`ArgumentNullException`/`ArgumentException`); `^[a-z][a-z0-9_]{1,31}:(read|write|admin)$` (`FormatException` otherwise): a resource of 2 to 32 characters
  (lower-case letter first, then lower-case letters, digits, `_`), a colon, and the action `read`, `write` or `admin`. `Resource` = before the colon, `Action` = after. Equality ordinal by value.
  (The pattern is a .NET regex used as written: `$` also matches before a final line feed.)
- `RedirectUri(Uri value)` — checks in order: non-null (`ArgumentNullException`); absolute; scheme `https`, or `http` only when `value.IsLoopback`; no fragment (`Fragment.Length > 0` refused)
  (all `ArgumentException`). Equality by `Uri.Compare` on the absolute URI, ordinal.
- `TokenPolicy(TimeSpan maximumLifetime, int maximumScopes)` — lifetime in (0, 24 h] (`HardMaximumLifetime` = 24 h); scopes in [1, 20] (all `ArgumentOutOfRangeException`).
- `Role` enum (Reader, Contributor, Administrator, Owner); `Permission` `[Flags]` enum (None = 0, Read = 1, Write = 2, Delete = 4, Grant = 8).
  `RolePermissions.Of(role)`: Reader = Read; Contributor = Read | Write; Administrator = Read | Write | Delete; Owner = Read | Write | Delete | Grant;
  an undefined role throws `ArgumentOutOfRangeException`.
- `RoleAssignment(Guid subjectId, Role role, DateOnly validFrom, DateOnly? validUntil)` — subject not `Guid.Empty` (`ArgumentException`); role defined (`ArgumentOutOfRangeException`);
  when present, `validUntil >= validFrom` (`ArgumentException`; a one-day grant is allowed). `IsActiveOn(day)` = `day >= ValidFrom` and (no end or `day <= ValidUntil`), both ends inclusive.

## Entities
- `ClientApplication(id, displayName, redirectUris, allowedScopes)` — id non-null; display name not null/blank and length ≤ 80 (`ArgumentOutOfRangeException` for the length); both collections non-null
  and non-empty (`ArgumentException`); redirect URIs must be distinct by `RedirectUri` equality (`ArgumentException`); duplicate scopes are silently collapsed.
  - `AcceptsRedirectTo(Uri candidate)` — true when the candidate is absolute and its `AbsoluteUri` string is exactly equal to a registered one (no prefix or wildcard matching); a `null` candidate is not guarded.
  - `MayRequest(Scope)` — membership in the allowed scopes.
- `AccessToken(tokenId, clientId, subjectId, scopes, issuedAt, expiresAt)` — token and subject ids not `Guid.Empty` (`ArgumentException`); client id and scopes non-null; at least one scope
  (`ArgumentException`); `expiresAt > issuedAt`; lifetime `expiresAt − issuedAt <= 24 h` (`ArgumentException`). Scopes are stored as a set (duplicates collapse). Time zone offsets are not checked.
  `IsExpiredAt(now)` = `now >= ExpiresAt`; `RemainingLifetimeAt(now)` = zero once expired, else `ExpiresAt − now`; `Authorizes(required, now)` = not expired and the scope is carried.
- `TokenIssuer(TokenPolicy policy)` — policy non-null (`ArgumentNullException`).
  `Issue(client, subjectId, requested, now, requestedLifetime)`: the requested scopes are collected into a set; the first one the client may not request throws `ScopeNotAllowedException`
  (carries `Scope`; derives from `Exception`); more distinct scopes than `policy.MaximumScopes` throws `InvalidOperationException` (checked after the per-scope test);
  the lifetime is silently capped at `policy.MaximumLifetime`; the token is then built with a new random id, so a non-positive requested lifetime, an empty scope request or an empty subject id surface as
  `ArgumentException` from `AccessToken`.
