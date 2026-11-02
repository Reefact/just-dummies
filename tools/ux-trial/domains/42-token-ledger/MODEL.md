# Token ledger — model

Namespace `TokenLedger.Domain` (`Domain/`). The ledger of one fungible token, kept off-chain in the manner of an ERC-20
contract: balances, allowances, mint and burn under a supply cap, per-account nonces, and a fee on every transaction.
Amounts are **unsigned 256-bit integers of base units** (`BigInteger` in [0, 2^256 − 1]); human amounts and prices are
exact decimals. Nothing ever wraps: a result past 2^256 − 1 or below 0 is refused.

## Value objects
- `Address(string)` — exactly `0x` + 40 **lower-case** hex digits, else `FormatException`. `Zero` (`0x000…0`) is a valid
  value (source of mints, destination of burns) but owns no account; `IsZero`. `Parse(text)` trims and lower-cases
  (accepts an EIP-55 mixed-case form without checking its checksum), refuses a `0X` prefix.
- `TokenAmount(BigInteger baseUnits)` — [0, 2^256 − 1] (`MaxBaseUnits`), else `ArgumentOutOfRangeException`. `Zero`, `Max`
  (as an allowance: unlimited). `Add`, `Subtract`, `MultiplyBy(ulong)` throw `OverflowException` outside the range;
  `CanAdd`; comparable.
- `ExactDecimal(BigInteger mantissa, int scale)` — value = mantissa × 10^−scale; scale in [0, 80] (`MaxScale`), mantissa
  unbounded and signed. **Normalised on construction**: trailing zeros stripped while scale > 0, zero is `0` at scale 0 —
  so `(1500, 3)` becomes `(15, 1)`, and equality is by (mantissa, scale). `Parse` accepts `-?digits(.digits)?` only
  (invariant, no exponent, no grouping, no `.5` or `5.`), more than 80 significant fractional digits → `FormatException`.
  `Add`, `Subtract`, `Negate`; `Multiply` exact (`OverflowException` if the normalised product needs > 80 fractional
  digits); `Divide(divisor, scale)` truncated toward zero (`DivideByZeroException`, scale in [0, 80]); `Truncate(scale)`;
  `ToScaledInteger(scale)` refused when scale < `Scale`; `IsInteger`, `Sign`; comparable; `ToString` canonical (`-0.00123`).
- `TokenDefinition(name, symbol, decimals, supplyCap)` — name 1..50 chars, not blank, no leading/trailing whitespace;
  symbol `^[A-Z][A-Z0-9]{1,10}$`; decimals [0, 36]; supply cap > 0 **and a whole number of tokens** (a multiple of
  10^decimals base units). `OneToken` = 10^decimals.
  `ToBaseUnits(human)`: negative → `ArgumentOutOfRangeException`; **more fractional digits than `decimals`** (counted after
  normalisation, so `1.50` is fine at 1 decimal) → `ArgumentException`; beyond 2^256 − 1 → `ArgumentOutOfRangeException`.
  `ParseAmount(text)` = `ToBaseUnits(ExactDecimal.Parse(text))`; `ToHuman(amount)` exact; `Format(amount)` → `"1234.5678 EXT"`.
- `TokenPrice(perToken, quoteCurrency)` — price of one whole token: > 0, at most 18 fractional digits (`ArgumentException`);
  currency `^[A-Z]{3}$`. `ValueOf(amount, token)` exact; `TokensFor(quoteAmount, token)` base units bought, rounded down,
  quote ≥ 0, `OverflowException` past 2^256 − 1.
- `GasSchedule(transfer, approve, transferFrom, mint, burn, newHolderSurcharge)` — each base cost in
  [21 000 (`IntrinsicGas`), 30 000 000 (`BlockGasLimit`)]; surcharge [0, 1 000 000]; **`transferFrom ≥ transfer`**;
  **max(transfer, transferFrom, mint) + surcharge ≤ 30 000 000**. `Standard` = 35 000 / 30 000 / 45 000 / 40 000 / 32 000 / +20 000.
  `GasFor(kind, createsHolder)`: the three allowance kinds cost `approve`; the surcharge applies only to Transfer,
  TransferFrom and Mint that credit a non-zero amount to an account whose balance was zero.
- `TransactionEnvelope(sender, UInt128 nonce, ulong gasLimit, TokenAmount gasPrice, ulong deadlineBlock)` — sender not the
  zero address (`ArgumentException`); gas limit in [21 000, 30 000 000]; gas price ≥ 0 base units per gas (0 allowed here);
  **gasLimit × gasPrice ≤ 2^256 − 1**. `MaxFee` = gasLimit × gasPrice. Deadline is inclusive.
- `Receipt(kind, sender, UInt128 nonce, ulong blockNumber, ulong gasUsed, TokenAmount gasPrice)` — defined kind; sender not
  zero; gas used in [21 000, 30 000 000]; **gasUsed × gasPrice ≤ 2^256 − 1**. `Fee` = gasUsed × gasPrice.
- `TransferRecord(from, to, amount, ulong blockNumber)` — the ERC-20 `Transfer` event; from and to **not both zero**;
  `IsMint` (from zero), `IsBurn` (to zero). Zero amounts are legal.
- Enums `TransactionKind` (Transfer, Approve, IncreaseAllowance, DecreaseAllowance, TransferFrom, Mint, Burn) and
  `RefusalReason` (NonceTooLow, NonceTooHigh, NonceExhausted, DeadlinePassed, GasPriceTooLow, ZeroAddress, NotMinter,
  GasLimitTooLow, CapExceeded, AllowanceExceeded, AllowanceOverflow, InsufficientBalance, InsufficientFundsForFee; no zero member).

## Entities
- `Account(address)` — not the zero address. Starts at balance 0, `NextNonce` 0 (`UInt128`); `IsHolder` when balance > 0.
  Only the ledger changes it.
- `LedgerRefusedException(reason, message)` — an `InvalidOperationException` carrying a defined `RefusalReason`.

## Aggregate: `Ledger(token, minter, feeCollector, initialSupply, gasSchedule, minimumGasPrice, ulong genesisBlock)`
- Construction: minter and fee collector not zero (`ArgumentException`); **initialSupply ≤ token.SupplyCap**
  (`ArgumentOutOfRangeException`); **minimumGasPrice × gasSchedule.Transfer ≤ token.SupplyCap** (`ArgumentException`).
  The initial supply is credited to the minter (a mint record at the genesis block). The minter and collector may coincide.
- State: `CurrentBlock`, `TotalSupply`, `RemainingMintable` (cap − supply), `Accounts`, `Receipts`, `Transfers`.
  Invariant: sum of balances = `TotalSupply` ≤ cap; fees move tokens, they never create or destroy them.
- Queries: `BalanceOf`, `NextNonceOf` (0 for an unseen address), `AllowanceOf(owner, spender)` (0 when unset),
  `HumanBalanceOf` (exact decimal), `ValueOf(address, price)`, `MarketCapitalisation(price)`.
- `AdvanceBlocks(count)`: count ≥ 1 and the block number stays ≤ `ulong.MaxValue`, else `ArgumentOutOfRangeException`.
- Transactions — `Transfer(env, to, amount)`, `Approve(env, spender, amount)` (replaces), `IncreaseAllowance(env, spender, added)`,
  `DecreaseAllowance(env, spender, subtracted)`, `TransferFrom(env, from, to, amount)` (spends the sender's allowance over
  `from`; an allowance of exactly 2^256 − 1 is unlimited and not decreased), `Mint(env, to, amount)`, `Burn(env, amount)`
  (the sender's own balance). Each returns a `Receipt` stamped with `CurrentBlock`.
- Checks, in this order; the first failure throws `LedgerRefusedException` and **nothing changes, nonce included**:
  1. nonce: below the sender's next → `NonceTooLow`; above → `NonceTooHigh`; next = `UInt128.MaxValue` → `NonceExhausted`;
  2. `CurrentBlock > DeadlineBlock` → `DeadlinePassed`;
  3. gas price < `MinimumGasPrice` → `GasPriceTooLow`;
  4. a zero `to` / `spender` / `from` → `ZeroAddress`;
  5. Mint by anyone but the minter → `NotMinter`;
  6. gas the operation needs (schedule + new-holder surcharge) > gas limit → `GasLimitTooLow`;
  7. operation: Mint past the cap → `CapExceeded`; IncreaseAllowance past 2^256 − 1 → `AllowanceOverflow`; DecreaseAllowance
     below 0 → `AllowanceExceeded`; TransferFrom over the allowance → `AllowanceExceeded`, then over `from`'s balance →
     `InsufficientBalance`; Transfer and Burn over the sender's balance → `InsufficientBalance`;
  8. the sender's balance, minus what the operation debits from the sender (credits to it do not count), must cover
     `MaxFee` = gasLimit × gasPrice → else `InsufficientFundsForFee`.
- On acceptance: the movement is applied and logged, then the fee **gasUsed × gasPrice** (gas actually used, not the limit)
  moves from the sender to the fee collector (logged as a transfer when > 0), and the sender's nonce increases by one.
- Null arguments throw `ArgumentNullException`.
