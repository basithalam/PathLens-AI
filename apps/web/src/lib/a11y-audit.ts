/**
 * H4.2 axe-core audit helper — run in dev console or Playwright.
 * Usage (browser console):
 *   import { runAxe } from "@/lib/a11y-audit"; await runAxe();
 * Or: npx playwright test with inject script.
 */
export async function runAxe(root: ParentNode = document): Promise<void> {
  if (process.env.NODE_ENV === "production") {
    console.warn("axe audit is for dev only");
    return;
  }
  try {
    const axe = await import("axe-core");
    const results = await axe.run(root, {
      runOnly: { type: "tag", values: ["wcag2a", "wcag2aa"] },
      resultTypes: ["violations"],
    });
    if (results.violations.length === 0) {
      console.log("a11y: 0 violations — good");
    } else {
      console.table(results.violations.map(v => ({ id: v.id, impact: v.impact, description: v.description, nodes: v.nodes.length })));
      results.violations.forEach(v => console.warn(`[${v.impact}] ${v.id}: ${v.help}`, v.nodes));
    }
  } catch (e) {
    console.error("axe not available — npm i -D axe-core and retry", e);
  }
}
