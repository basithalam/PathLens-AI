"use client";

import { useState } from "react";
import { login, type SessionUser } from "@/lib/auth";

const ROLES = [
  {
    role: "Reviewer",
    mark: "R",
    email: "reviewer@pathlens.local",
    password: "Reviewer@123",
    can: "Verify, dispute or reject reports and comparisons",
  },
  {
    role: "Admin",
    mark: "A",
    email: "admin@pathlens.local",
    password: "Admin@123",
    can: "Register cameras, read analytics and reputation",
  },
  {
    role: "User",
    mark: "U",
    email: "user@pathlens.local",
    password: "User@123",
    can: "Ordinary signed-in reporter — no review rights",
  },
] as const;

/**
 * One sign-in surface for both staff consoles. It only calls the existing
 * POST /api/auth/login path in lib/auth — no new auth behaviour.
 */
export function SignInPanel({
  title = "Sign in to the review console",
  blurb = "Human review is what turns a claim into something other people can rely on. Every decision you make here is written to an audit trail with your name, the previous value and your note.",
  defaultRole = "Reviewer",
  onSignedIn,
}: {
  title?: string;
  blurb?: string;
  defaultRole?: string;
  onSignedIn: (user: SessionUser) => void;
}) {
  const preset = ROLES.find((entry) => entry.role === defaultRole) ?? ROLES[0];
  const [email, setEmail] = useState<string>(preset.email);
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);
    const result = await login(email, password);
    setBusy(false);
    if (result.ok) {
      setPassword("");
      onSignedIn(result.user);
    } else {
      setError(result.message);
    }
  }

  function fillRole(role: (typeof ROLES)[number]) {
    setEmail(role.email);
    setPassword(role.password);
    setError(null);
  }

  return (
    <section className="auth" aria-labelledby="signin-heading">
      <aside className="auth__aside">
        <h2 id="signin-heading">Reviewer &amp; Admin access</h2>
        <p>
          {blurb}
        </p>
        <ul className="auth__points">
          <li>AI output is stored as an observation, never overwritten by a review.</li>
          <li>Verification, dispute and rejection are separate outcomes.</li>
          <li>Each action records actor, previous value, new value, note and time.</li>
          <li>A plain user account gets 403 on every review endpoint.</li>
        </ul>
        <p className="auth__footnote">
          The token lives in this browser only and is sent as a Bearer header. Reviewer rights are enforced by
          the API, not by this page — hiding the buttons is never the check.
        </p>
      </aside>

      <div className="auth__panel">
        <h3>{title}</h3>
        <p>
          Accounts are seeded for local development only — change them before any other deployment.
        </p>

        <div className="auth__roles" role="group" aria-label="Demo accounts — fills the form">
          {ROLES.map((role) => {
            const active = role.email === email;
            return (
              <button
                key={role.role}
                type="button"
                className="auth__role"
                aria-pressed={active}
                onClick={() => fillRole(role)}
              >
                <span className="auth__role-mark" aria-hidden="true">
                  {role.mark}
                </span>
                <span className="auth__role-body">
                  <strong>{role.role}</strong>
                  <span>{role.email}</span>
                  <span>{role.can}</span>
                </span>
                <span className="auth__role-tag">use</span>
              </button>
            );
          })}
        </div>

        <form onSubmit={onSubmit} className="stack-sm" noValidate>
          <div className="field">
            <label htmlFor="signin-email">Email</label>
            <input
              id="signin-email"
              name="email"
              type="email"
              autoComplete="username"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              required
            />
          </div>
          <div className="field">
            <label htmlFor="signin-password">Password</label>
            <input
              id="signin-password"
              name="password"
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              placeholder="Demo password from the account above"
              required
            />
          </div>
          {error && (
            <p role="alert" className="status-error">
              {error}
            </p>
          )}
          <div className="auth__actions">
            <button type="submit" className="btn btn--primary" disabled={busy}>
              {busy ? "Signing in…" : "Sign in"}
            </button>
            <button type="button" className="btn btn--ghost" onClick={() => fillRole(preset)}>
              Fill {preset.role.toLowerCase()} demo
            </button>
          </div>
        </form>

        <p className="auth__note">
          Sign-in issues are rate limited to 5 per minute by the API. A wrong password never reveals
          whether the email exists.
        </p>
      </div>
    </section>
  );
}

export function SessionBar({
  user,
  onLogout,
  hint,
}: {
  user: SessionUser;
  onLogout: () => void;
  hint?: string;
}) {
  return (
    <p className="session-bar">
      <span className="session-bar__who">
        <span className="session-bar__avatar" aria-hidden="true">
          {user.displayName.slice(0, 1).toUpperCase()}
        </span>
        {user.displayName}
      </span>
      <span className={`pill ${user.role === "Admin" ? "pill--on" : "pill--off"}`}>{user.role}</span>
      {hint ? <span className="hint">{hint}</span> : null}
      <span className="spacer" />
      <button type="button" className="btn btn--ghost btn--sm" onClick={onLogout}>
        Log out
      </button>
    </p>
  );
}
