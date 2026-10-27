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
          <p>
            PathLens AI never labels a route safe just because no obstacle was
            reported. Unknown means unknown.
          </p>
        </footer>
      </body>
    </html>
  );
}
