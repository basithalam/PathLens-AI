"use client";

import Link from "next/link";
import { LocaleSwitch } from "@/lib/i18n";

export function SiteHeader() {
  return (
    <header className="site-header">
      <div className="site-header__inner">
        <Link href="/" className="brand">
          <span className="brand__mark" aria-hidden="true">
            <span />
          </span>
          PathLens AI
        </Link>
        <nav aria-label="Main" className="site-nav">
          <Link href="/map">Map</Link>
          <Link href="/routes">Routes</Link>
          <Link href="/reports">Reports</Link>
          <Link href="/report/new" className="nav-cta">
            Report
          </Link>
          <Link href="/reviewer">Reviewer</Link>
          <Link href="/admin">Admin</Link>
        </nav>
        <LocaleSwitch />
      </div>
    </header>
  );
}
