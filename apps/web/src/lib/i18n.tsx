"use client";

import { createContext, useContext, useEffect, useState, type ReactNode } from "react";

type Locale = "en" | "bn";
type Messages = Record<string, string>;

const en: Messages = {
  "app.title": "PathLens AI",
  "app.tagline": "Evidence-Aware Accessibility Intelligence",
  "map.title": "Accessibility map",
  "report.title": "Report an obstacle",
  "route.title": "Plan a route",
  "unknown.note": "Unknown means unknown — not proven clear.",
};

const bn: Messages = {
  "app.title": "PathLens AI",
  "app.tagline": "প্রমাণ-ভিত্তিক প্রবেশগম্যতা",
  "map.title": "প্রবেশগম্যতা মানচিত্র",
  "report.title": "বাধা রিপোর্ট করুন",
  "route.title": "পথ পরিকল্পনা",
  "unknown.note": "অজানা মানে অজানা — পরিষ্কার প্রমাণিত নয়।",
};

const dictionaries: Record<Locale, Messages> = { en, bn };

const Ctx = createContext<{ locale: Locale; t: (k: string) => string; setLocale: (l: Locale) => void }>({
  locale: "en",
  t: (k) => k,
  setLocale: () => {},
});

export function LocaleProvider({ children }: { children: ReactNode }) {
  const [locale, setLocaleState] = useState<Locale>("en");
  useEffect(() => {
    const saved = localStorage.getItem("pathlens.locale") as Locale | null;
    if (saved === "bn" || saved === "en") setLocaleState(saved);
  }, []);
  const setLocale = (l: Locale) => {
    setLocaleState(l);
    localStorage.setItem("pathlens.locale", l);
  };
  const t = (k: string) => dictionaries[locale][k] ?? k;
  return <Ctx.Provider value={{ locale, t, setLocale }}>{children}</Ctx.Provider>;
}

export function useLocale() {
  return useContext(Ctx);
}

export function LocaleSwitch() {
  const { locale, setLocale } = useLocale();
  return (
    <div style={{ display: "inline-flex", gap: 6, alignItems: "center" }} aria-label="Language switch">
      <button
        type="button"
        onClick={() => setLocale("en")}
        aria-pressed={locale === "en"}
        className={locale === "en" ? "btn btn--primary btn--sm" : "btn btn--ghost btn--sm"}
      >
        EN
      </button>
      <button
        type="button"
        onClick={() => setLocale("bn")}
        aria-pressed={locale === "bn"}
        className={locale === "bn" ? "btn btn--primary btn--sm" : "btn btn--ghost btn--sm"}
      >
        বাংলা
      </button>
    </div>
  );
}
