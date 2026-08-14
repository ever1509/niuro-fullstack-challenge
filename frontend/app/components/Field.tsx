import type { ReactNode } from "react";

type FieldProps = {
  label: string;
  htmlFor: string;
  error?: string;
  hint?: string;
  children: ReactNode;
  className?: string;
};

export function Field({ label, htmlFor, error, hint, children, className }: FieldProps) {
  return (
    <div className={className}>
      <label htmlFor={htmlFor} className="block text-sm font-medium">
        {label}
      </label>
      <div className="mt-1.5">{children}</div>
      {error ? (
        // Announced to screen readers as soon as it appears, not only seen.
        <p role="alert" className="mt-1.5 text-sm text-danger">
          {error}
        </p>
      ) : hint ? (
        <p className="mt-1.5 text-sm text-muted">{hint}</p>
      ) : null}
    </div>
  );
}

export const inputClassName =
  "w-full rounded-lg border border-line bg-surface px-3 py-2 text-sm outline-none " +
  "focus:border-accent focus:ring-2 focus:ring-accent/25 disabled:opacity-60";

export function Fieldset({ legend, children }: { legend: string; children: ReactNode }) {
  return (
    <fieldset className="border-t border-line pt-6 first:border-t-0 first:pt-0">
      <legend className="sr-only">{legend}</legend>
      <h2 className="mb-4 text-sm font-semibold uppercase tracking-wide text-muted">{legend}</h2>
      {children}
    </fieldset>
  );
}
