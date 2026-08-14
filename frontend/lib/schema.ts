import { z } from "zod";

import { US_STATES } from "./states";

const stateCodes = US_STATES.map((state) => state.code) as unknown as [string, ...string[]];

/**
 * Client-side validation, kept deliberately in step with the API contract.
 *
 * This exists for the person filling the form, not for safety: the same rules are enforced
 * again on the server, because anything typed here can be bypassed by calling the API directly.
 */
export const applicationSchema = z.object({
  firstName: z.string().trim().min(1, "First name is required").max(100),
  lastName: z.string().trim().min(1, "Last name is required").max(100),
  street: z.string().trim().min(1, "Street address is required").max(200),
  city: z.string().trim().min(1, "City is required").max(100),
  state: z.enum(stateCodes, { message: "Select a state" }),
  postalCode: z
    .string()
    .trim()
    .regex(/^\d{5}(-\d{4})?$/, "Enter a ZIP code, for example 94105"),
  companyName: z.string().trim().min(1, "Company name is required").max(200),
  requestedAmount: z
    .number({ message: "Enter an amount" })
    .positive("Amount must be greater than zero")
    .max(1_000_000, "Amount must not exceed $1,000,000"),
  ssn: z
    .string()
    .trim()
    .regex(/^\d{3}-?\d{2}-?\d{4}$/, "Enter a 9-digit SSN, for example 123-45-6789"),
});

export type ApplicationFormValues = z.infer<typeof applicationSchema>;
