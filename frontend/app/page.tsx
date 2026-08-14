"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";

import { Field, Fieldset, inputClassName } from "./components/Field";
import { submitApplication } from "@/lib/api";
import { rememberDecision } from "@/lib/decision-store";
import { applicationSchema, type ApplicationFormValues } from "@/lib/schema";
import { US_STATES } from "@/lib/states";

export default function ApplyPage() {
  const router = useRouter();
  const [submissionError, setSubmissionError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<ApplicationFormValues>({
    resolver: zodResolver(applicationSchema),
    defaultValues: { state: "CA" },
  });

  async function onSubmit(values: ApplicationFormValues) {
    setSubmissionError(null);

    try {
      const decision = await submitApplication(values);
      rememberDecision(decision);
      router.push(decision.decision === "Approved" ? "/approved" : "/denied");
    } catch (error) {
      setSubmissionError(
        error instanceof Error ? error.message : "Something went wrong. Please try again.",
      );
    }
  }

  return (
    <div>
      <h1 className="text-2xl font-semibold tracking-tight">Apply for a loan</h1>
      <p className="mt-2 text-muted">
        Tell us about yourself and your business. You will get a decision straight away.
      </p>

      {submissionError && (
        <div
          role="alert"
          className="mt-6 rounded-lg border border-danger/30 bg-danger-surface px-4 py-3 text-sm text-danger"
        >
          {submissionError}
        </div>
      )}

      <form
        onSubmit={handleSubmit(onSubmit)}
        noValidate
        className="mt-8 space-y-6 rounded-xl border border-line bg-surface p-6 shadow-sm"
      >
        <Fieldset legend="Applicant">
          <div className="grid gap-4 sm:grid-cols-2">
            <Field label="First name" htmlFor="firstName" error={errors.firstName?.message}>
              <input id="firstName" autoComplete="given-name" className={inputClassName} {...register("firstName")} />
            </Field>

            <Field label="Last name" htmlFor="lastName" error={errors.lastName?.message}>
              <input id="lastName" autoComplete="family-name" className={inputClassName} {...register("lastName")} />
            </Field>

            <Field
              label="Social Security Number"
              htmlFor="ssn"
              error={errors.ssn?.message}
              hint="We use this to find an application you already have."
              className="sm:col-span-2"
            >
              <input
                id="ssn"
                inputMode="numeric"
                autoComplete="off"
                placeholder="123-45-6789"
                maxLength={11}
                className={`${inputClassName} font-mono`}
                {...register("ssn")}
                onChange={(event) => setValue("ssn", formatSsn(event.target.value), { shouldValidate: false })}
              />
            </Field>
          </div>
        </Fieldset>

        <Fieldset legend="Address">
          <div className="grid gap-4 sm:grid-cols-6">
            <Field label="Street" htmlFor="street" error={errors.street?.message} className="sm:col-span-6">
              <input id="street" autoComplete="address-line1" className={inputClassName} {...register("street")} />
            </Field>

            <Field label="City" htmlFor="city" error={errors.city?.message} className="sm:col-span-3">
              <input id="city" autoComplete="address-level2" className={inputClassName} {...register("city")} />
            </Field>

            <Field label="State" htmlFor="state" error={errors.state?.message} className="sm:col-span-1">
              <select id="state" autoComplete="address-level1" className={inputClassName} {...register("state")}>
                {US_STATES.map((state) => (
                  <option key={state.code} value={state.code}>
                    {state.code}
                  </option>
                ))}
              </select>
            </Field>

            <Field label="ZIP code" htmlFor="postalCode" error={errors.postalCode?.message} className="sm:col-span-2">
              <input id="postalCode" autoComplete="postal-code" className={inputClassName} {...register("postalCode")} />
            </Field>
          </div>
        </Fieldset>

        <Fieldset legend="Loan">
          <div className="grid gap-4 sm:grid-cols-2">
            <Field label="Company name" htmlFor="companyName" error={errors.companyName?.message}>
              <input id="companyName" autoComplete="organization" className={inputClassName} {...register("companyName")} />
            </Field>

            <Field
              label="Requested amount"
              htmlFor="requestedAmount"
              error={errors.requestedAmount?.message}
              hint="Up to $1,000,000."
            >
              <div className="relative">
                <span className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-sm text-muted">
                  $
                </span>
                <input
                  id="requestedAmount"
                  type="number"
                  inputMode="decimal"
                  step="0.01"
                  min="0"
                  className={`${inputClassName} pl-7`}
                  {...register("requestedAmount", { valueAsNumber: true })}
                />
              </div>
            </Field>
          </div>
        </Fieldset>

        <div className="flex items-center gap-4 border-t border-line pt-6">
          <button
            type="submit"
            disabled={isSubmitting}
            className="rounded-lg bg-accent px-5 py-2.5 text-sm font-medium text-accent-foreground
                       transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isSubmitting ? "Checking…" : "Submit application"}
          </button>
          <p className="text-sm text-muted">A decision takes a moment.</p>
        </div>
      </form>
    </div>
  );
}

/** Formats as the applicant types, so the value on screen matches the example given. */
function formatSsn(value: string): string {
  const digits = value.replace(/\D/g, "").slice(0, 9);

  if (digits.length <= 3) return digits;
  if (digits.length <= 5) return `${digits.slice(0, 3)}-${digits.slice(3)}`;
  return `${digits.slice(0, 3)}-${digits.slice(3, 5)}-${digits.slice(5)}`;
}
