import { getSustainedLoadOptions } from '../config/options.js';
import { thresholds } from '../config/thresholds.js';
import { userJourney } from '../workflows/user-journey.js';

export const options = {
  scenarios: {
    sustained_load: {
      ...getSustainedLoadOptions(),
      exec: 'default',
    },
  },
  thresholds,
};

export default function () {
  userJourney();
}
