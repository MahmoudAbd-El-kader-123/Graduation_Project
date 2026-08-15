import { check } from 'k6';

function parseBody(response) {
  try {
    return { parsed: true, body: response.json() };
  } catch (_) {
    return { parsed: false, body: null };
  }
}

export function checkApiResponse(response, label) {
  const result = parseBody(response);
  return check(response, {
    [`${label}: status is 2xx`]: (res) => res.status >= 200 && res.status < 300,
    [`${label}: body is JSON`]: () => result.parsed,
    [`${label}: success is true`]: () => result.parsed && result.body.success === true,
  });
}

export function checkPagedResponse(response, label) {
  const result = parseBody(response);
  const basePassed = checkApiResponse(response, label);
  const pagePassed = check(response, {
    [`${label}: items is an array`]: () =>
      result.parsed && result.body.data && Array.isArray(result.body.data.items),
    [`${label}: totalCount is valid`]: () =>
      result.parsed &&
      result.body.data &&
      typeof result.body.data.totalCount === 'number' &&
      result.body.data.totalCount >= 0,
  });
  return basePassed && pagePassed;
}

export function checkEntityResponse(response, label) {
  const result = parseBody(response);
  const basePassed = checkApiResponse(response, label);
  const entityPassed = check(response, {
    [`${label}: entity exists`]: () => result.parsed && Boolean(result.body.data),
  });
  return basePassed && entityPassed;
}
