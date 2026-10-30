import type { Metadata } from "next";
import "./globals.css";
import { SiteHeader } from "@/components/SiteHeader";

export const metadata: Metadata = {
  title: "PathLens AI — Evidence-Aware Accessibility Intelligence",
  description:
    "PathLens AI helps people judge whether a pedestrian path is likely usable, using photo evidence compared over time — and says plainly when it does not know.",
};

export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en">
      <body>
        <SiteHeader />
        <main className="page">{children}</main>
        <footer className="site-footer">
          <div className="site-footer__inner">
            <p>
              PathLens AI never labels a route safe just because no obstacle was reported.
              <strong> Unknown means unknown.</strong>
            </p>
            <p className="site-footer__meta">Evidence-aware · Built for the Meta Global AI Hackathon 2026</p>
          </div>
        </footer>
      </body>
    </html>
  );
}
