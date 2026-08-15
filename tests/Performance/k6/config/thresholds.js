export const thresholds = {
  http_req_failed: ['rate<0.01'],
  http_req_duration: ['p(95)<1000', 'p(99)<2000'],
  checks: ['rate>0.95'],
};
