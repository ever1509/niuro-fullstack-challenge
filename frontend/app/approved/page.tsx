"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect } from "react";

import { forgetDecision, useStoredDecision } from "@/lib/decision-store";

export default function ApprovedPage() {
  const router = useRouter();
  const decision = useStoredDecision();
  const isApproved = decision?.decision === "Approved";

  useEffect(() => {
    // Landing here without having applied is meaningless; send them to the form.
    if (!isApproved) {
      router.replace("/");
    }
  }, [isApproved, router]);

  if (!decision || !isApproved) return null;

  return (
    <div className="rounded-xl border border-line bg-surface p-8 shadow-sm">
      <p className="inline-flex rounded-full bg-success-surface px-3 py-1 text-sm font-medium text-success">
        Approved
      </p>

      <h1 className="mt-4 text-2xl font-semibold tracking-tight">
        {decision.isReturningCustomer
          ? "Your application has been updated"
          : "Your application has been approved"}
      </h1>

      <p className="mt-2 text-muted">
        {decision.isReturningCustomer
          ? "We already had an application on file for you, so we updated it with the details you just sent instead of opening a second one."
          : "We have everything we need. Keep your reference number for any questions."}
      </p>

      <dl className="mt-6 grid gap-px overflow-hidden rounded-lg border border-line bg-line text-sm sm:grid-cols-2">
        <div className="bg-surface p-4">
          <dt className="text-muted">Application reference</dt>
          <dd className="mt-1 font-mono text-xs break-all">{decision.applicationId}</dd>
        </div>
        <div className="bg-surface p-4">
          <dt className="text-muted">Customer reference</dt>
          <dd className="mt-1 font-mono text-xs break-all">{decision.customerId}</dd>
        </div>
      </dl>

      <Link
        href="/"
        onClick={forgetDecision}
        className="mt-8 inline-flex rounded-lg border border-line px-5 py-2.5 text-sm font-medium transition hover:border-accent"
      >
        Start another application
      </Link>
    </div>
  );
}
