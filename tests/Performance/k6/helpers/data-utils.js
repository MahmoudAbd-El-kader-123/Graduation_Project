import { sleep } from 'k6';

export function randomItem(items) {
  if (!Array.isArray(items) || items.length === 0) {
    throw new Error('Test data arrays must contain at least one item.');
  }
  return items[Math.floor(Math.random() * items.length)];
}

export function thinkTime() {
  const configuredMin = Number(__ENV.THINK_TIME_MIN || 1);
  const configuredMax = Number(__ENV.THINK_TIME_MAX || 5);
  const min = Number.isFinite(configuredMin) && configuredMin >= 0 ? configuredMin : 1;
  const max = Number.isFinite(configuredMax) && configuredMax >= min ? configuredMax : Math.max(min, 5);
  sleep(Math.random() * (max - min) + min);
}

function uniqueName(prefix) {
  return `${prefix}-${__VU}-${__ITER}-${Date.now()}`;
}

export function generateVendorName() {
  return uniqueName('k6-vendor');
}

export function generateProductName() {
  return uniqueName('k6-product');
}

export function generateVendorMapping(vendorId) {
  const suffix = `${__VU}-${__ITER}-${Date.now()}`;
  return {
    vendorId,
    systemField: `k6-system-${suffix}`,
    excelColumn: `k6-column-${suffix}`,
  };
}
