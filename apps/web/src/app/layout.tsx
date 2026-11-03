import type { Metadata, Viewport } from "next";
import "./globals.css";
import { SiteHeader } from "@/components/SiteHeader";

export const metadata: Metadata = {
  title: "PathLens AI — Evidence-Aware Accessibility Intelligence",
  description:
    "PathLens AI helps people judge whether a pedestrian path is likely usable, using photo evidence compared over time — and says plainly when it does not know.",
  manifest: "/manifest.json",
};

export const viewport: Viewport = {
  width: "device-width",
  initialScale: 1,
  themeColor: [
    { media: "(prefers-color-scheme: light)", color: "#f5f8f7" },
    { media: "(prefers-color-scheme: dark)", color: "#080f0d" },
  ],
};

/* Applied before first paint so the chosen theme never flashes. */
const THEME_BOOTSTRAP = `try{var t=localStorage.getItem("pathlens.theme");if(!t)t=matchMedia("(prefers-color-scheme: dark)").matches?"dark":"light";document.documentElement.dataset.theme=t;}catch(e){}`;

export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en" data-theme="light" suppressHydrationWarning>
      <head>
        <link rel="apple-touch-icon" href="/marker-icon.png" />
        <meta name="apple-mobile-web-app-capable" content="yes" />
        <script dangerouslySetInnerHTML={{ __html: THEME_BOOTSTRAP }} />
      </head>
      <body>
        <a href="#main" className="skip-link">
          Skip to content
        </a>
        <SiteHeader />
        <main id="main" className="page">
          {children}
        </main>
        <footer className="site-footer">
          <div className="site-footer__inner">
            <p>
              PathLens AI never labels a route safe just because no obstacle was reported.
              <strong> Unknown means unknown.</strong>
            </p>
            <p className="site-footer__meta">
              <span>Evidence-aware</span>
              <span>Human-verified separately from AI</span>
              <span>Meta Global AI Hackathon 2026</span>
            </p>
          </div>
        </footer>
      </body>
    </html>
  );
}
