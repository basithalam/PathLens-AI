"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useState } from "react";

export function SiteHeader() {
  const pathname = usePathname();
  const [open, setOpen] = useState(false);

  const isActive = (href: string) => pathname === href || (href !== "/" && pathname?.startsWith(href));

  return (
    <header className="site-header">
      <div className="site-header__inner">
        <Link href="/" className="brand" onClick={() => setOpen(false)}>
          <span className="brand__mark" aria-hidden="true">
            <span />
          </span>
          <span>
            PathLens<span style={{ color: "var(--brand)" }}> AI</span>
          </span>
        </Link>

        <button
          className={`mobile-menu-btn ${open ? "open" : ""}`}
          aria-label={open ? "Close menu" : "Open menu"}
          aria-expanded={open}
          onClick={() => setOpen((v) => !v)}
        >
          <span />
        </button>

        <nav aria-label="Main" className={`site-nav ${open ? "open" : ""}`}>
          <Link href="/map" className={isActive("/map") ? "active" : ""} onClick={() => setOpen(false)}>Map</Link>
          <Link href="/routes" className={isActive("/routes") ? "active" : ""} onClick={() => setOpen(false)}>Routes</Link>
          <Link href="/reports" className={isActive("/reports") ? "active" : ""} onClick={() => setOpen(false)}>Reports</Link>
          <Link href="/report/new" className="nav-cta" onClick={() => setOpen(false)}>+ Report</Link>
          <Link href="/reviewer" className={isActive("/reviewer") ? "active" : ""} onClick={() => setOpen(false)}>Reviewer</Link>
          <Link href="/admin" className={isActive("/admin") ? "active" : ""} onClick={() => setOpen(false)}>Admin</Link>
        </nav>
      </div>
    </header>
  );
}
