import { check } from 'k6';
import http from 'k6/http';

export function authenticate() {
  const response = http.post(
    `${__ENV.BASE_URL}/api/auth/login`,
    JSON.stringify({
      email: __ENV.TEST_USER_EMAIL,
      password: __ENV.TEST_USER_PASSWORD,
    }),
    {
      headers: { 'Content-Type': 'application/json' },
      tags: { name: 'POST /api/auth/login' },
    },
  );

  let body = {};
  try {
    body = response.json();
  } catch (_) {
    // Checks below report malformed responses without terminating the VU.
  }

  check(response, {
    'login status is 200': (res) => res.status === 200,
    'login success is true': () => body.success === true,
    'login returned token': () => Boolean(body.data && body.data.token),
  });

  return body.data && body.data.token ? body.data.token : '';
}
