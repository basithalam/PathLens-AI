"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useState } from "react";

const NAV = [
  { href: "/map", label: "Map" },
  { href: "/routes", label: "Routes" },
  { href: "/reports", label: "Reports" },
];

const TEAM_NAV = [
  { href: "/reviewer", label: "Reviewer" },
  { href: "/admin", label: "Admin" },
];

function isActive(pathname: string, href: string): boolean {
  return pathname === href || pathname.startsWith(`${href}/`);
}

export function SiteHeader() {
  const pathname = usePathname();
  const [navOpen, setNavOpen] = useState(false);
  const [theme, setTheme] = useState<"light" | "dark">("light");

  useEffect(() => {
    const stored = window.localStorage.getItem("pathlens.theme");
    const initial =
      stored === "dark" || stored === "light"
        ? stored
        : window.matchMedia("(prefers-color-scheme: dark)").matches
          ? "dark"
          : "light";
    setTheme(initial);
    document.documentElement.dataset.theme = initial;
  }, []);

  useEffect(() => {
    setNavOpen(false);
  }, [pathname]);

  function toggleTheme() {
    const next = theme === "dark" ? "light" : "dark";
    setTheme(next);
    document.documentElement.dataset.theme = next;
    window.localStorage.setItem("pathlens.theme", next);
  }

  return (
    <header className="site-header">
      <div className="site-header__inner">
        <Link href="/" className="brand">
          <span className="brand__mark" aria-hidden="true">
            <span />
          </span>
          <span className="brand__name">
            PathLens AI
            <em>Evidence-aware access</em>
          </span>
        </Link>

        <button
          type="button"
          className="icon-btn nav-toggle"
          aria-expanded={navOpen}
          aria-controls="site-nav"
          onClick={() => setNavOpen((open) => !open)}
        >
          <span aria-hidden="true">{navOpen ? "✕" : "☰"}</span>
          {navOpen ? "Close" : "Menu"}
        </button>

        <div className="header-tools">
          <button
            type="button"
            className="icon-btn"
            onClick={toggleTheme}
            aria-pressed={theme === "dark"}
            title={theme === "dark" ? "Switch to light theme" : "Switch to dark theme"}
          >
            <span aria-hidden="true">{theme === "dark" ? "☀" : "☾"}</span>
            <span className="sr-only">Toggle dark theme</span>
          </button>
          <Link href="/report/new" className="btn btn--primary btn--sm nav-cta">
            + Report
          </Link>
        </div>

        <nav aria-label="Main" className="site-nav" id="site-nav" data-open={navOpen}>
          {NAV.map((item) => (
            <Link
              key={item.href}
              href={item.href}
              aria-current={isActive(pathname, item.href) ? "page" : undefined}
            >
              {item.label}
            </Link>
          ))}
          <span className="nav-divider" aria-hidden="true" />
          {TEAM_NAV.map((item) => (
            <Link
              key={item.href}
              href={item.href}
              aria-current={isActive(pathname, item.href) ? "page" : undefined}
            >
              {item.label}
            </Link>
          ))}
        </nav>
      </div>
    </header>
  );
}
