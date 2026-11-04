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
    can: "Register cameras, read city analytics and reputation",
  },
  {
    role: "User",
    mark: "U",
    email: "user@pathlens.local",
    password: "User@123",
    can: "Ordinary signed-in reporter — the API answers 403 on review",
  },
] as const;

/**
 * One sign-in surface for both staff consoles. It only calls the existing
 * POST /api/auth/login path in lib/auth — no new auth behaviour.
 *
 * The demo is deliberately open: every seeded credential is shown on the card and one tap signs in,
 * so a judge never has to ask for a password. A real deployment rotates these through the
 * Auth:Seed* configuration keys (see docs/SECURITY.md); with no secret set nothing changes.
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
  const [busy, setBusy] = useState<string | null>(null);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy("form");
    const result = await login(email, password);
    setBusy(null);
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

  async function quickSignIn(role: (typeof ROLES)[number]) {
    setError(null);
    setBusy(role.role);
    setEmail(role.email);
    const result = await login(role.email, role.password);
    setBusy(null);
    if (result.ok) {
      setPassword("");
      onSignedIn(result.user);
    } else {
      setPassword(role.password);
      setError(result.message);
    }
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
          This demo is intentionally open. Pick an account and sign in with one tap — no password to ask for.
          They are seeded hackathon accounts on a demo dataset, not real users.
        </p>

        <div className="auth__roles" role="group" aria-label="Demo accounts — one tap signs in">
          {ROLES.map((role) => {
            const active = role.email === email;
            return (
              <div key={role.role} className={`auth__role${active ? " auth__role--active" : ""}`}>
                <span className="auth__role-mark" aria-hidden="true">
                  {role.mark}
                </span>
                <span className="auth__role-body">
                  <strong>{role.role}</strong>
                  <span className="mono">{role.email}</span>
                  <span className="mono">{role.password}</span>
                  <span>{role.can}</span>
                </span>
                <button
                  type="button"
                  className="btn btn--brand btn--sm"
                  onClick={() => quickSignIn(role)}
                  disabled={busy !== null}
                >
                  {busy === role.role ? "Signing in…" : "Sign in"}
                </button>
              </div>
            );
          })}
        </div>

        <form onSubmit={onSubmit} className="stack-sm" noValidate>
          <p className="auth__hint">
            Or type them — the same three accounts, nothing hidden from the panel above.
          </p>
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
              placeholder="Reviewer@123 · Admin@123 · User@123"
              required
            />
          </div>
          {error && (
            <p role="alert" className="status-error">
              {error}
            </p>
          )}
          <div className="auth__actions">
            <button type="submit" className="btn btn--primary" disabled={busy !== null}>
              {busy === "form" ? "Signing in…" : "Sign in"}
            </button>
            <button type="button" className="btn btn--ghost" onClick={() => fillRole(preset)}>
              Fill {preset.role.toLowerCase()} demo
            </button>
          </div>
        </form>

        <p className="auth__note">
          Sign-in is rate limited to 5 attempts per minute, and a wrong password never reveals whether the
          email exists. Reviewer and Admin see the same queue; only Admin can register cameras.
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
