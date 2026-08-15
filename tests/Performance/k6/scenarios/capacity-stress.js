import { getCapacityStressOptions } from '../config/options.js';
import { thresholds } from '../config/thresholds.js';
import { userJourney } from '../workflows/user-journey.js';

export const options = {
  scenarios: {
    capacity_stress: {
      ...getCapacityStressOptions(),
      exec: 'default',
    },
  },
  thresholds,
};

export default function () {
  userJourney();
}
