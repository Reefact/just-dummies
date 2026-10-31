# Pharmaceutical Batch Genealogy — model

Namespace `BatchGenealogy.Domain`. Traces which lots a product came from, and which products a
non-conforming material reached, through a directed acyclic graph of blends, splits and processes.

## Value objects
- `LotNumber(string)` — `AAA-123456` or sub-lot `AAA-123456.07` (prefix 2-4 upper-case letters, 6 digits,
  sub-lot 01-99). `BaseLot`, `SubLotIndex`, `IsSubLot`, `SubLot(int 1..99)`.
- `MaterialCode(string)` — `MAT-` + 5 digits.
- `Quantity(decimal, QuantityUnit)` — 0 <= amount <= 1e9; units mg/g/kg (mass) and mL/L (volume).
  `Add`, `Subtract` (never negative), `Scale(0..1)`, `ConvertTo`, `CompareTo`; mass and volume never mix.
  Equality is by base amount (1 kg == 1000 g).
- `LotQuantity(LotNumber, Quantity)` — quantity strictly positive.
- `ProvenanceLink(source, target, transformationId, share)` — source != target, share in (0, 1].
- `TraceEntry(lot, depth, share, exposedQuantity)` — depth >= 1, share in (0, 1].

## Entities
- `Lot(number, material, initialQuantity, manufacturedOn, expiresOn)` — quantity > 0, expiry strictly after
  manufacture, shelf life <= 3653 days. Starts `Quarantined`; `Release()` only from Quarantined;
  `Quarantine()` refused once Rejected; `Reject()` final. `Consume(q, on)` requires Released, not expired,
  not before manufacture, same dimension, q <= remaining.
- `Transformation(id, kind, inputs, outputs, performedOn)` — id `TRF-` + 6 digits; Blend: >=2 in, 1 out;
  Split: 1 in, >=2 out, all outputs sub-lots of the input's base lot; Process: >=1 in, 1 out. No lot twice
  on a side nor on both sides; one dimension throughout; total output <= total input.
  `TotalInput`, `TotalOutput`, `Loss`, `Yield`, `ShareOf(input)` (input / total input, homogeneous mixing).

## Aggregate: `BatchGenealogy(siteCode)` (four upper-case letters)
- `Register(Lot)` — refuses a duplicate number. `Get(LotNumber)` — `KeyNotFoundException` if unknown.
- `Record(Transformation)` — all-or-nothing: ids unique; every lot registered; inputs consumable on the date;
  each output produced once, never consumed before, registered with exactly the produced quantity and
  manufactured on the transformation date; a split keeps the material. Then consumes inputs and adds
  provenance links. Fresh outputs keep the graph acyclic by construction.
- `ShareOf(ancestor, lot)` — fraction of `lot` coming from `ancestor`, summed over every path (merges count).
- `TraceUpstream(product)` — ancestors with share and the quantity of each contained in the product.
- `TraceDownstream(material)` — descendants with share and exposed quantity (share x lot initial quantity).
- `Contain(nonConforming, minimumShare in (0,1])` — rejects the material, quarantines every non-rejected
  descendant carrying at least that share, returns them.
