"use client";

import { useEffect, useState } from "react";
import { clearSession, getSessionUser, type SessionUser } from "@/lib/auth";
import { SessionBar, SignInPanel } from "@/components/auth/SignInPanel";
import { AdminCamerasPanel } from "@/components/reviewer/AdminCamerasPanel";
import { AdminStatsPanel } from "@/components/admin/AdminStatsPanel";
import { ReputationPanel } from "@/components/admin/ReputationPanel";

/**
 * One sign-in surface for the whole admin page: the panels below only mount with
 * an Admin session, so they never show a raw 401 to a judge who has not signed in yet.
 */
export function AdminConsole() {
  const [user, setUser] = useState<SessionUser | null>(null);
  const [checked, setChecked] = useState(false);

  useEffect(() => {
    setUser(getSessionUser());
    setChecked(true);
  }, []);

  if (!checked) {
    return (
      <div className="skeleton" aria-live="polite">
        <div className="sk-line sk-line--title" />
        <div className="sk-card" />
        <div className="sk-card" />
      </div>
    );
  }

  if (!user) {
    return (
      <SignInPanel
        title="Sign in as Admin"
        blurb="Registering a live camera feed is a trust decision, not a configuration detail. A camera cannot be enabled without a permission note, and its snapshots stay un-verified candidates until a reviewer says otherwise."
        defaultRole="Admin"
        visibleRoles="admin"
        heading="Admin access"
        onSignedIn={setUser}
      />
    );
  }

  if (user.role !== "Admin") {
    return (
      <>
        <SessionBar user={user} onLogout={() => { clearSession(); setUser(null); }} />
        <p role="alert" className="callout callout--warn">
          <strong>{user.displayName} is signed in as {user.role}.</strong> Cameras, city analytics and
          reporter reputation are Admin-only — the API answers 403 for this role. Sign out and use the Admin
          demo account to open them.
        </p>
      </>
    );
  }

  return (
    <>
      <SessionBar
        user={user}
        onLogout={() => {
          clearSession();
          setUser(null);
        }}
        hint="Cameras, analytics and reputation are visible to Admin only."
      />
      <AdminCamerasPanel />
      <AdminStatsPanel />
      <ReputationPanel />
    </>
  );
}
