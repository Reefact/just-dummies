// Test fixture for Conventional Comments Toolkit page captures (PR b).
// Not meant to be merged.

export function total(items) {
  let sum = 0;
  for (const item of items) {
    sum += item.price * item.quantity;
  }
  return sum;
}

export function average(items) {
  return items.length === 0 ? 0 : total(items) / items.length;
}
