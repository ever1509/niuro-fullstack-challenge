"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect } from "react";

import { forgetDecision, useStoredDecision } from "@/lib/decision-store";

export default function DeniedPage() {
  const router = useRouter();
  const decision = useStoredDecision();
  const isDenied = decision?.decision === "Denied";

  useEffect(() => {
    if (!isDenied) {
      router.replace("/");
    }
  }, [isDenied, router]);

  if (!decision || !isDenied) return null;

  return (
    <div className="rounded-xl border border-line bg-surface p-8 shadow-sm">
      <p className="inline-flex rounded-full bg-danger-surface px-3 py-1 text-sm font-medium text-danger">
        Not approved
      </p>

      <h1 className="mt-4 text-2xl font-semibold tracking-tight">
        We could not approve this application
      </h1>

      <p className="mt-2 text-muted">
        {decision.reasons.length > 1
          ? "There was more than one reason. Both are listed below so you are not left guessing."
          : "Here is why."}
      </p>

      <ul className="mt-6 space-y-3">
        {decision.reasons.map((reason) => (
          <li
            key={reason.code}
            className="rounded-lg border border-line bg-danger-surface/40 px-4 py-3 text-sm"
          >
            {reason.reason}
          </li>
        ))}
      </ul>

      <p className="mt-6 text-sm text-muted">
        Nothing was saved. If you think something here is wrong, get in touch and we will take
        another look.
      </p>

      <Link
        href="/"
        onClick={forgetDecision}
        className="mt-8 inline-flex rounded-lg border border-line px-5 py-2.5 text-sm font-medium transition hover:border-accent"
      >
        Back to the form
      </Link>
    </div>
  );
}
