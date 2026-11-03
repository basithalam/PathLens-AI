export default function ReportsLoading() {
  return (
    <div className="skeleton" aria-live="polite" aria-busy="true">
      <div className="sk-line sk-line--title" />
      <div className="sk-card" />
      <div className="sk-card" />
      <div className="sk-card" />
      <p className="hint">Loading reports for this page…</p>
    </div>
  );
}
