import Link from "next/link";

export function SiteHeader() {
  return (
    <header className="site-header">
      <Link href="/" className="brand">
        PathLens AI
      </Link>
      <nav aria-label="Main">
        <Link href="/">Home</Link>
        <Link href="/reports">Reports</Link>
        <Link href="/report/new">Report an obstacle</Link>
        <Link href="/map">Map</Link>
        <Link href="/routes">Plan a route</Link>
      </nav>
    </header>
  );
}
