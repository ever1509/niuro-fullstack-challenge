import type { ApplicationFormValues } from "./schema";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5207";

export type DenialReason = {
  code: string;
  reason: string;
};

export type Decision = {
  decision: "Approved" | "Denied";
  applicationId: string | null;
  customerId: string | null;
  isReturningCustomer: boolean;
  reasons: DenialReason[];
};

/**
 * Submits the form and returns the decision.
 *
 * A denial comes back as a normal 200 with `decision: "Denied"` — it is an answer, not a
 * failure — so only a genuine transport or server error throws here.
 */
export async function submitApplication(values: ApplicationFormValues): Promise<Decision> {
  const response = await fetch(`${API_BASE_URL}/api/loan-applications`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(values),
  });

  if (!response.ok) {
    throw new Error(await describeFailure(response));
  }

  return (await response.json()) as Decision;
}

async function describeFailure(response: Response): Promise<string> {
  try {
    const problem = await response.json();
    if (typeof problem?.detail === "string") return problem.detail;
    if (typeof problem?.title === "string") return problem.title;
  } catch {
    // Not a JSON body; fall through to the generic message.
  }

  return `The application could not be submitted (HTTP ${response.status}).`;
}
