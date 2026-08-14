'use strict';

/**
 * Stand-in for the partner system that our approved applications are forwarded to.
 *
 * It has no dependencies on purpose: `node server.js` is the whole setup, which keeps the
 * run instructions to one line and means there is no lockfile to review.
 *
 * Contract
 *   POST   /customers               create a record (idempotent by customerId)
 *   PUT    /customers/:customerId   update an existing record
 *   GET    /customers               list everything received, for inspection
 *   GET    /health                  liveness
 *
 * Test scaffolding, clearly marked and not part of the contract:
 *   POST   /__control/fail-next?count=N   reject the next N writes, to demonstrate retries
 */

const http = require('node:http');

const PORT = Number(process.env.PORT) || 4000;

/** customerId -> record */
const customers = new Map();
let failuresRemaining = 0;

const server = http.createServer(async (request, response) => {
  const url = new URL(request.url, `http://${request.headers.host}`);
  const route = `${request.method} ${url.pathname}`;

  try {
    if (route === 'GET /health') {
      return send(response, 200, { status: 'ok', customers: customers.size });
    }

    if (route === 'GET /customers') {
      return send(response, 200, [...customers.values()]);
    }

    if (route === 'POST /__control/fail-next') {
      failuresRemaining = Number(url.searchParams.get('count')) || 1;
      log('control', `will reject the next ${failuresRemaining} write(s)`);
      return send(response, 200, { failuresRemaining });
    }

    if (route === 'POST /customers') {
      return await handleWrite(request, response, 'created');
    }

    if (request.method === 'PUT' && url.pathname.startsWith('/customers/')) {
      const customerId = url.pathname.slice('/customers/'.length);
      return await handleWrite(request, response, 'updated', customerId);
    }

    send(response, 404, { error: `No route for ${route}` });
  } catch (error) {
    log('error', error.message);
    send(response, 400, { error: error.message });
  }
});

async function handleWrite(request, response, action, customerIdFromPath) {
  const payload = await readJson(request);
  const customerId = customerIdFromPath ?? payload.customerId;

  if (!customerId) {
    return send(response, 400, { error: 'customerId is required' });
  }

  if (failuresRemaining > 0) {
    failuresRemaining -= 1;
    log('reject', `${action} ${customerId} - simulated outage, ${failuresRemaining} left`);
    return send(response, 503, { error: 'Service unavailable (simulated)' });
  }

  // Storing by id makes a repeated delivery harmless: the same message arriving twice
  // leaves exactly one record, so the sender is free to retry.
  const existed = customers.has(customerId);
  customers.set(customerId, { ...payload, customerId, receivedAt: new Date().toISOString() });

  log(
    action,
    `${payload.firstName ?? ''} ${payload.lastName ?? ''}`.trim() +
      ` | ${customerId} | ${formatAmount(payload.requestedAmount)}` +
      (existed ? ' | overwrote existing record' : '')
  );

  send(response, 200, { customerId, action });
}

function readJson(request) {
  return new Promise((resolve, reject) => {
    let body = '';
    request.on('data', (chunk) => {
      body += chunk;
      if (body.length > 1_000_000) reject(new Error('Payload too large'));
    });
    request.on('error', reject);
    request.on('end', () => {
      if (!body) return resolve({});
      try {
        resolve(JSON.parse(body));
      } catch {
        reject(new Error('Body is not valid JSON'));
      }
    });
  });
}

function send(response, statusCode, body) {
  const json = JSON.stringify(body);
  response.writeHead(statusCode, {
    'content-type': 'application/json',
    'content-length': Buffer.byteLength(json)
  });
  response.end(json);
}

function formatAmount(amount) {
  return typeof amount === 'number'
    ? amount.toLocaleString('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 })
    : 'n/a';
}

function log(action, message) {
  const time = new Date().toISOString().slice(11, 19);
  console.log(`${time}  ${action.padEnd(8)}  ${message}`);
}

server.listen(PORT, () => {
  console.log(`Mock external service listening on http://localhost:${PORT}`);
  console.log('Waiting for approved applications...\n');
});
