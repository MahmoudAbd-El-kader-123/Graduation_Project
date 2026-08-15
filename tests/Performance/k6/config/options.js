function positiveInteger(value, fallback) {
  const parsed = parseInt(value, 10);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback;
}

export function getSustainedLoadOptions() {
  const targetVUs = positiveInteger(__ENV.TARGET_VUS, 100);

  return {
    executor: 'ramping-vus',
    stages: [
      { duration: __ENV.RAMP_UP_DURATION || '2m', target: targetVUs },
      { duration: __ENV.SUSTAINED_DURATION || '30m', target: targetVUs },
      { duration: __ENV.RAMP_DOWN_DURATION || '1m', target: 0 },
    ],
  };
}

export function getCapacityStressOptions() {
  const stepDuration = __ENV.CAPACITY_STEP_DURATION || '3m';
  const steps = (__ENV.CAPACITY_STEPS || '100,250,500,750,1000,1250,1500')
    .split(',')
    .map((value) => positiveInteger(value.trim(), 0))
    .filter((value) => value > 0);

  if (steps.length === 0) {
    throw new Error('CAPACITY_STEPS must contain at least one positive integer.');
  }

  return {
    executor: 'ramping-vus',
    stages: [
      ...steps.map((target) => ({ duration: stepDuration, target })),
      { duration: '1m', target: 0 },
    ],
  };
}
