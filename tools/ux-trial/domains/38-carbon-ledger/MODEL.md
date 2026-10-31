# Carbon ledger — domain model (`CarbonLedger.Domain`)

A registry's double-entry journal of identified carbon credits: units are issued, transferred and retired,
and no unit is ever held twice or used twice.

## Value objects and entities
- `CreditSerial(registry, projectId, vintage, unitNumber)` — one tonne's identity. Registry `^[A-Z]{2,5}$`,
  projectId 1..999 999, vintage 1990..2100, unitNumber 1..999 999 999 999. Value equality;
  `ToString`/`Parse` round-trip `VCS-1234-2019-000000000042`.
- `Holder(holderId, legalName, countryCode)` — id `^[A-Z0-9]{4,12}$`, non-blank name ≤ 200, ISO alpha-2 country.
- `Account(accountId, holder, kind, openedOn)` — id `^[A-Z]{2}-\d{6}$`, **prefixed by the holder's country**;
  `AccountKind`: `Issuance` (registry source), `Holding`, `Retirement` (terminal).
- `RetirementClaim(beneficiary, purpose, reportingYear)` — non-blank texts ≤ 500, year 2000..2100.
- `HoldingPeriod(serial, accountId, from, until?)` — `until` (null = still held) not before `from`.
- `JournalEntry(sequence, recordedAt, kind, debit, credit, serials, claim?)` — sequence ≥ 1; debit ≠ credit;
  ≥ 1 serial, no null, no duplicate; account kinds match `EntryKind`: Issuance = Issuance→Holding,
  Transfer = Holding→Holding, Retirement = Holding→Retirement. Only a retirement carries a claim, and it must;
  its reporting year is not before the latest vintage retired. Balanced by construction: `Quantity` leaves
  the debit and enters the credit.
- `LedgerRuleViolationException` — every refused registry operation.

## Aggregate: `CarbonRegistryLedger(registry, issuanceAccount)`
The issuance account must be of kind `Issuance`. The **journal** is the append-only source of truth;
holders, retired set and balances are **derived** state.
- `OpenAccount` — refuses a second issuance account or a duplicate id.
- `Issue(serials, to, at)` — serials of this registry only, each issued once; issuance balance goes negative.
- `Transfer(serials, from, to, at)` — every unit active and held by `from`.
- `Retire(serials, from, retirementAccount, claim, at)` — same, then the units are locked forever:
  any later transfer or retirement of them is refused.
- Every post: `at` not before the last entry nor before either account's `openedOn`; unknown accounts refused.
- Queries: `Balance`, `HolderOf`, `IsRetired`, `UnitsHeldBy`, `IssuedQuantity`, `RetiredQuantity`,
  `HistoryOf(serial)` (holding periods rebuilt from the journal), `Journal`.
- `IsConsistent()` — balances sum to zero (conservation) and the derived holders and balances match a
  replay of the journal.
