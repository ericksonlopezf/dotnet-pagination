#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Stryker.NET Summary & Quality Gate Processor
Parses Stryker.NET mutation testing outputs, generates GitHub Step Summary,
persists mutation-summary.json metadata, and posts commit statuses via GitHub API.
"""

import os
import sys
import glob
import json
import re
import urllib.request
import urllib.error
from datetime import datetime, timezone

# Ensure UTF-8 output on all platforms (Windows / Linux)
if sys.stdout and hasattr(sys.stdout, "reconfigure"):
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
if sys.stderr and hasattr(sys.stderr, "reconfigure"):
    try:
        sys.stderr.reconfigure(encoding="utf-8")
    except Exception:
        pass


def load_thresholds(config_path="stryker-config.json"):
    default_thresholds = {"high": 100, "low": 98, "break": 95}
    if os.path.exists(config_path):
        try:
            with open(config_path, "r", encoding="utf-8") as f:
                data = json.load(f)
                stryker_cfg = data.get("stryker-config", data)
                thresholds = stryker_cfg.get("thresholds", {})
                return {
                    "high": thresholds.get("high", default_thresholds["high"]),
                    "low": thresholds.get("low", default_thresholds["low"]),
                    "break": thresholds.get("break", default_thresholds["break"]),
                }
        except Exception as e:
            print(f"[WARN] Error reading thresholds from {config_path}: {e}", file=sys.stderr)
    return default_thresholds


def find_latest_report(stryker_output_dir="StrykerOutput"):
    if not os.path.exists(stryker_output_dir):
        return None, None

    # Look for mutation-report.json first, then mutation-report.html
    json_candidates = glob.glob(os.path.join(stryker_output_dir, "**", "mutation-report.json"), recursive=True)
    html_candidates = glob.glob(os.path.join(stryker_output_dir, "**", "mutation-report.html"), recursive=True)

    json_path = None
    html_path = None

    if json_candidates:
        json_candidates.sort(key=os.path.getmtime, reverse=True)
        json_path = json_candidates[0]

    if html_candidates:
        html_candidates.sort(key=os.path.getmtime, reverse=True)
        html_path = html_candidates[0]

    return json_path, html_path


def parse_mutation_data(json_path, html_path):
    # Strategy 1: Read JSON file
    if json_path and os.path.exists(json_path):
        try:
            with open(json_path, "r", encoding="utf-8") as f:
                return json.load(f), os.path.dirname(json_path)
        except Exception as e:
            print(f"[WARN] Failed to parse {json_path}: {e}", file=sys.stderr)

    # Strategy 2: Extract JSON from HTML report
    if html_path and os.path.exists(html_path):
        try:
            with open(html_path, "r", encoding="utf-8") as f:
                content = f.read()
            # Regex match for embedded app.report
            m = re.search(r'app\.report\s*=\s*(\{.*?\});\s*(?:function|document|app\.)', content, re.DOTALL)
            if not m:
                # Alternative regex for Mutation Test Report Elements
                m = re.search(r'document\.querySelector\([\'\"]mutation-test-report-app[\'\"]\)\.report\s*=\s*(\{.*?\});', content, re.DOTALL)
            if m:
                return json.loads(m.group(1)), os.path.dirname(html_path)
        except Exception as e:
            print(f"[WARN] Failed to extract JSON from {html_path}: {e}", file=sys.stderr)

    return None, None


def compute_metrics(report_data, thresholds):
    killed = 0
    survived = 0
    timeout = 0
    no_coverage = 0
    compile_error = 0
    ignored = 0
    total = 0

    files = report_data.get("files", {})
    for file_path, file_info in files.items():
        mutants = file_info.get("mutants", [])
        for mutant in mutants:
            total += 1
            status = mutant.get("status")
            if status == "Killed":
                killed += 1
            elif status == "Survived":
                survived += 1
            elif status == "Timeout":
                timeout += 1
            elif status == "NoCoverage":
                no_coverage += 1
            elif status == "CompileError":
                compile_error += 1
            elif status == "Ignored":
                ignored += 1

    detected = killed + timeout
    undetected = survived + no_coverage
    valid_mutants = detected + undetected

    if valid_mutants > 0:
        score = (detected / valid_mutants) * 100.0
    else:
        score = 100.0

    high_th = thresholds.get("high", 100)
    low_th = thresholds.get("low", 98)
    break_th = thresholds.get("break", 95)

    if score >= high_th:
        status_name = "HIGH"
        status_badge = "✅ HIGH"
    elif score >= low_th:
        status_name = "LOW"
        status_badge = "🟡 LOW"
    elif score >= break_th:
        status_name = "WARNING"
        status_badge = "🟠 WARNING"
    else:
        status_name = "FAILED"
        status_badge = "❌ FAILED"

    passed = (score >= break_th)

    return {
        "score": round(score, 2),
        "total": total,
        "killed": killed,
        "survived": survived,
        "timeout": timeout,
        "no_coverage": no_coverage,
        "compile_error": compile_error,
        "ignored": ignored,
        "valid_mutants": valid_mutants,
        "status_name": status_name,
        "status_badge": status_badge,
        "passed": passed,
    }


def post_commit_status(commit_sha, repo, token, metrics, thresholds, run_id):
    if not (commit_sha and repo and token):
        print("[INFO] Skipping commit status update (missing SHA, repo or GITHUB_TOKEN).")
        return

    state = "success" if metrics["passed"] else "failure"
    description = f"Score: {metrics['score']:.2f}% (Break: {thresholds['break']}%) - {metrics['status_name']}"
    target_url = f"https://github.com/{repo}/actions/runs/{run_id}" if run_id else ""

    url = f"https://api.github.com/repos/{repo}/statuses/{commit_sha}"
    payload = {
        "state": state,
        "context": "stryker/mutation-gate",
        "description": description[:140],
        "target_url": target_url,
    }

    try:
        req = urllib.request.Request(
            url,
            data=json.dumps(payload).encode("utf-8"),
            headers={
                "Authorization": f"Bearer {token}",
                "Accept": "application/vnd.github.v3+json",
                "User-Agent": "Stryker-Summary-Action",
                "Content-Type": "application/json",
            },
            method="POST",
        )
        with urllib.request.urlopen(req) as resp:
            if resp.status in (200, 201):
                print(f"[INFO] Successfully posted commit status ({state}) to {url}")
            else:
                print(f"[WARN] Commit status returned status {resp.status}")
    except Exception as e:
        print(f"[WARN] Failed to post commit status: {e}", file=sys.stderr)


def main():
    commit_sha = os.environ.get("GITHUB_SHA", "local-execution")
    repo = os.environ.get("GITHUB_REPOSITORY", "")
    token = os.environ.get("GITHUB_TOKEN", "")
    run_id = os.environ.get("GITHUB_RUN_ID", "")
    profile = os.environ.get("STRYKER_PROFILE", "Standard")
    step_summary_path = os.environ.get("GITHUB_STEP_SUMMARY", "")
    now_utc = datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M:%S UTC")

    print(f"=== Stryker.NET Summary Processor ===")
    print(f"Commit SHA: {commit_sha}")
    print(f"Profile:    {profile}")
    print(f"Timestamp:  {now_utc}")

    thresholds = load_thresholds()
    print(f"Thresholds: High={thresholds['high']}%, Low={thresholds['low']}%, Break={thresholds['break']}%")

    json_path, html_path = find_latest_report()
    if not json_path and not html_path:
        print("[ERROR] No Stryker mutation reports found in StrykerOutput directory!", file=sys.stderr)
        summary_md = f"""# 🧬 Stryker.NET Mutation Testing Quality Gate

❌ **FAILED: No Stryker output or report was generated.**

| Metric | Value |
|---|---|
| **Status** | ❌ FAILED |
| **Commit SHA** | `{commit_sha}` |
| **Execution Date** | {now_utc} |
| **Profile** | `{profile}` |
"""
        if step_summary_path:
            with open(step_summary_path, "a", encoding="utf-8") as f:
                f.write(summary_md)
        sys.exit(1)

    print(f"Found report: json={json_path}, html={html_path}")
    report_data, report_dir = parse_mutation_data(json_path, html_path)
    if not report_data:
        print("[ERROR] Failed to extract report data from Stryker output files.", file=sys.stderr)
        sys.exit(1)

    metrics = compute_metrics(report_data, thresholds)

    print("\n--- Mutation Testing Results ---")
    print(f"Mutation Score:   {metrics['score']:.2f}%")
    print(f"Mutants Killed:   {metrics['killed']} / {metrics['valid_mutants']}")
    print(f"Mutants Survived: {metrics['survived']}")
    print(f"Mutants Timeout:  {metrics['timeout']}")
    print(f"Mutants NoCov:    {metrics['no_coverage']}")
    print(f"Compile Errors:   {metrics['compile_error']}")
    print(f"Ignored Mutants:  {metrics['ignored']}")
    print(f"Total Mutants:    {metrics['total']}")
    print(f"Quality Status:   {metrics['status_badge']}")
    print(f"Break Threshold:  {thresholds['break']}%")
    print(f"Gate Outcome:     {'PASSED' if metrics['passed'] else 'FAILED'}")

    # Generate metadata JSON
    metadata = {
        "commit_sha": commit_sha,
        "execution_date_utc": now_utc,
        "mutation_score": metrics["score"],
        "mutants_killed": metrics["killed"],
        "mutants_survived": metrics["survived"],
        "mutants_timeout": metrics["timeout"],
        "mutants_no_coverage": metrics["no_coverage"],
        "mutants_compile_error": metrics["compile_error"],
        "mutants_ignored": metrics["ignored"],
        "total_mutants": metrics["total"],
        "valid_mutants": metrics["valid_mutants"],
        "threshold_high": thresholds["high"],
        "threshold_low": thresholds["low"],
        "threshold_break": thresholds["break"],
        "status": metrics["status_name"],
        "status_badge": metrics["status_badge"],
        "passed": metrics["passed"],
        "profile": profile,
    }

    # Save metadata to root StrykerOutput and specific report directory
    os.makedirs("StrykerOutput", exist_ok=True)
    with open(os.path.join("StrykerOutput", "mutation-summary.json"), "w", encoding="utf-8") as f:
        json.dump(metadata, f, indent=2)

    if report_dir and os.path.exists(report_dir):
        with open(os.path.join(report_dir, "mutation-summary.json"), "w", encoding="utf-8") as f:
            json.dump(metadata, f, indent=2)

    # Post Commit Status to GitHub API
    post_commit_status(commit_sha, repo, token, metrics, thresholds, run_id)

    # Write Step Summary Markdown
    step_summary_md = f"""# 🧬 Stryker.NET Mutation Testing Quality Gate

### Result: {metrics['status_badge']} ({metrics['score']:.2f}%)

| Metric | Value |
|---|---|
| **Mutation Score** | **`{metrics['score']:.2f}%`** |
| **Mutants Killed** | `{metrics['killed']:,}` |
| **Total Mutants** | `{metrics['total']:,}` |
| **Threshold High** | `{thresholds['high']}%` |
| **Threshold Low** | `{thresholds['low']}%` |
| **Threshold Break** | `{thresholds['break']}%` |
| **Status** | {metrics['status_badge']} |
| **Commit SHA** | `{commit_sha}` |
| **Execution Date** | {now_utc} |
| **Profile** | `{profile}` |

<details>
<summary><b>📊 Detailed Mutation Breakdown</b></summary>

| Category | Count | Description |
|---|---|---|
| 🟢 **Killed** | `{metrics['killed']:,}` | Mutants caught and terminated by test suite |
| 🔴 **Survived** | `{metrics['survived']:,}` | Mutants not caught by test suite |
| ⏱️ **Timeout** | `{metrics['timeout']:,}` | Mutants causing infinite loops/timeouts (counted as detected) |
| ⚪ **No Coverage** | `{metrics['no_coverage']:,}` | Code not reached by any test |
| ⚠️ **Compile Error** | `{metrics['compile_error']:,}` | Syntactically invalid mutations discarded |
| 🔒 **Ignored** | `{metrics['ignored']:,}` | Excluded via `stryker-config.json` rules |
| 🎯 **Analyzed Mutants** | `{metrics['valid_mutants']:,}` | Total valid candidates evaluated |

</details>

> [!NOTE]
> **Quality Gate Policy:**
> - `>= 100%`: ✅ HIGH
> - `>= 98% && < 100%`: 🟡 LOW
> - `>= 95% && < 98%`: 🟠 WARNING (Quality gate passes)
> - `< 95%`: ❌ FAILED (Breaks workflow & blocks releases)
"""

    if step_summary_path:
        try:
            with open(step_summary_path, "a", encoding="utf-8") as f:
                f.write(step_summary_md)
            print(f"[INFO] Written GitHub Step Summary to {step_summary_path}")
        except Exception as e:
            print(f"[WARN] Failed to write Step Summary: {e}", file=sys.stderr)
    else:
        print("\n" + step_summary_md)

    if not metrics["passed"]:
        print(f"\n[ERROR] Mutation score {metrics['score']:.2f}% is below break threshold {thresholds['break']}%. Quality gate FAILED!", file=sys.stderr)
        sys.exit(1)

    print("\n[SUCCESS] Quality gate PASSED successfully.")
    sys.exit(0)


if __name__ == "__main__":
    main()
