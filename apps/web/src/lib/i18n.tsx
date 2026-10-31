// i18n removed — English only for hackathon judging (no toggle)
import type { ReactNode } from "react";

export function LocaleProvider({ children }: { children: ReactNode }) {
  return <>{children}</>;
}
export function useLocale() {
  return { locale: "en" as const, t: (k: string) => k, setLocale: () => {} };
}
export function LocaleSwitch() {
  return null;
}
