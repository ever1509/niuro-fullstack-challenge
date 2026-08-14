import { useSyncExternalStore } from "react";

import type { Decision } from "./api";

const STORAGE_KEY = "niuro.loans.decision";

/**
 * Hands the decision from the form to the result page.
 *
 * Session storage rather than the URL: a decision is not something to put in a link that can
 * be shared, bookmarked or logged by a proxy. It also survives a refresh, so reloading the
 * result page still shows the outcome instead of an empty screen.
 */
export function rememberDecision(decision: Decision): void {
  sessionStorage.setItem(STORAGE_KEY, JSON.stringify(decision));
}

export function forgetDecision(): void {
  sessionStorage.removeItem(STORAGE_KEY);
}

// getSnapshot must return the same reference until the stored value actually changes,
// otherwise React re-renders forever. The raw string is the thing worth comparing.
let cachedRaw: string | null = null;
let cachedDecision: Decision | null = null;

function getSnapshot(): Decision | null {
  const raw = sessionStorage.getItem(STORAGE_KEY);

  if (raw !== cachedRaw) {
    cachedRaw = raw;
    cachedDecision = parse(raw);
  }

  return cachedDecision;
}

/** There is no session storage on the server, so the first paint has no decision. */
function getServerSnapshot(): Decision | null {
  return null;
}

function subscribe(onStoreChange: () => void): () => void {
  window.addEventListener("storage", onStoreChange);
  return () => window.removeEventListener("storage", onStoreChange);
}

function parse(raw: string | null): Decision | null {
  if (!raw) return null;

  try {
    return JSON.parse(raw) as Decision;
  } catch {
    return null;
  }
}

/**
 * Reads the stored decision as an external store rather than copying it into state inside an
 * effect, which is what React expects for anything that lives outside the component tree.
 */
export function useStoredDecision(): Decision | null {
  return useSyncExternalStore(subscribe, getSnapshot, getServerSnapshot);
}
