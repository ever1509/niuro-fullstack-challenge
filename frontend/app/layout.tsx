import type { Metadata } from "next";
import Link from "next/link";
import { Geist, Geist_Mono } from "next/font/google";
import "./globals.css";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "Niuro Loans",
  description: "Apply for a business loan.",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html
      lang="en"
      className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}
    >
      <body className="min-h-full flex flex-col">
        <header className="border-b border-line bg-surface">
          <div className="mx-auto flex w-full max-w-3xl items-center justify-between px-6 py-4">
            <Link href="/" className="text-lg font-semibold tracking-tight">
              Niuro <span className="text-muted font-normal">Loans</span>
            </Link>
            <span className="text-sm text-muted">Business lending</span>
          </div>
        </header>

        <main className="mx-auto w-full max-w-3xl flex-1 px-6 py-10">{children}</main>

        <footer className="border-t border-line py-6">
          <p className="mx-auto w-full max-w-3xl px-6 text-sm text-muted">
            Demo application. Do not enter real personal information.
          </p>
        </footer>
      </body>
    </html>
  );
}
