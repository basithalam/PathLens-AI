// Merge deep parts A-E into user-manual.html (run: node scripts/merge-manual.mjs)
// Idempotent: restores the base from git first, so re-runs never double-merge.
import { readFileSync, writeFileSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import { execSync } from "node:child_process";
const dir = join(dirname(fileURLToPath(import.meta.url)), "..", "docs");
execSync("git checkout HEAD -- docs/user-manual.html", { cwd: join(dir, "..") });
let h = readFileSync(join(dir, "user-manual.html"), "utf8");
for (const f of ["user-manual-partA.html", "user-manual-partB.html", "user-manual-partC.html", "user-manual-partD.html", "user-manual-partE.html"]) {
  const part = readFileSync(join(dir, f), "utf8");
  h = h.replace('<div class="card" id="public">', '<div class="card">' + part + '</div><div class="card" id="public">');
}
h = h.replace('<p><a href="#start">1. Start here</a>', '<p><a href="#deep">Deep guide: A report - B route - C review - D admin - E AI</a><br><a href="#start">1. Start here</a>');
h = h.replace('<div class="card" id="public">', '<div id="deep"></div><div class="card" id="public">');
writeFileSync(join(dir, "user-manual.html"), h);
console.log("merged len=" + h.length);
