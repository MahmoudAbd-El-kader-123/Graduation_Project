import http from 'k6/http';

function params(token, endpointName) {
  return {
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
    tags: { name: endpointName },
  };
}

export function get(path, token, endpointName) {
  return http.get(`${__ENV.BASE_URL}${path}`, params(token, endpointName));
}

export function post(path, body, token, endpointName) {
  return http.post(
    `${__ENV.BASE_URL}${path}`,
    JSON.stringify(body),
    params(token, endpointName),
  );
}

export function put(path, body, token, endpointName) {
  return http.put(
    `${__ENV.BASE_URL}${path}`,
    JSON.stringify(body),
    params(token, endpointName),
  );
}

export function del(path, token, endpointName) {
  return http.del(`${__ENV.BASE_URL}${path}`, null, params(token, endpointName));
}
