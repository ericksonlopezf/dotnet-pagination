#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Stryker.NET Release Quality Gate Validator
Validates that the target commit or recent main branch has a passing Stryker.NET
mutation testing score (>= 95%) before allowing a release or package publication.
"""

import os
import sys
import json
import re
import urllib.request
import urllib.error
from datetime import datetime, timezone

# Ensure UTF-8 output on all platforms
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


def get_commit_status(repo, commit_sha, token):
    if not (repo and commit_sha and token):
        return None

    url = f"https://api.github.com/repos/{repo}/commits/{commit_sha}/status"
    req = urllib.request.Request(
        url,
        headers={
            "Authorization": f"Bearer {token}",
            "Accept": "application/vnd.github.v3+json",
            "User-Agent": "Stryker-Release-Validator",
        },
    )

    try:
        with urllib.request.urlopen(req) as resp:
            data = json.load(resp)
            statuses = data.get("statuses", [])
            for st in statuses:
                if st.get("context") == "stryker/mutation-gate":
                    return {
                        "state": st.get("state"),
                        "description": st.get("description", ""),
                        "target_url": st.get("target_url", ""),
                        "updated_at": st.get("updated_at", ""),
                        "source": "commit_status",
                    }
    except Exception as e:
        print(f"[DEBUG] Commit status query to {url} failed or not found: {e}")

    return None


def get_workflow_run_status(repo, commit_sha, token):
    if not (repo and token):
        return None

    # First check runs for specific commit_sha
    urls = []
    if commit_sha:
        urls.append(f"https://api.github.com/repos/{repo}/actions/workflows/stryker.yml/runs?head_sha={commit_sha}&per_page=5")
    # Fallback to latest main runs
    urls.append(f"https://api.github.com/repos/{repo}/actions/workflows/stryker.yml/runs?branch=main&status=completed&per_page=5")

    for url in urls:
        try:
            req = urllib.request.Request(
                url,
                headers={
                    "Authorization": f"Bearer {token}",
                    "Accept": "application/vnd.github.v3+json",
                    "User-Agent": "Stryker-Release-Validator",
                },
            )
            with urllib.request.urlopen(req) as resp:
                data = json.load(resp)
                runs = data.get("workflow_runs", [])
                for run in runs:
                    if run.get("conclusion") in ("success", "failure"):
                        return {
                            "conclusion": run.get("conclusion"),
                            "head_sha": run.get("head_sha"),
                            "created_at": run.get("created_at"),
                            "html_url": run.get("html_url"),
                            "run_id": run.get("id"),
                            "source": "workflow_run",
                        }
        except Exception as e:
            print(f"[DEBUG] Workflow run query to {url} failed: {e}")

    return None


def check_local_metadata(stryker_output_dir="StrykerOutput"):
    summary_path = os.path.join(stryker_output_dir, "mutation-summary.json")
    if os.path.exists(summary_path):
        try:
            with open(summary_path, "r", encoding="utf-8") as f:
                data = json.load(f)
                data["source"] = "local_summary_file"
                return data
        except Exception as e:
            print(f"[DEBUG] Failed to read {summary_path}: {e}")
    return None


def parse_score_from_desc(desc):
    # Format: "Score: 99.96% (Break: 95%) - LOW"
    m = re.search(r"Score:\s*([0-9]+(?:\.[0-9]+)?)\s*%", desc)
    if m:
        try:
            return float(m.group(1))
        except ValueError:
            pass
    return None


def main():
    target_sha = os.environ.get("GITHUB_SHA", "")
    repo = os.environ.get("GITHUB_REPOSITORY", "")
    token = os.environ.get("GITHUB_TOKEN", "")
    step_summary_path = os.environ.get("GITHUB_STEP_SUMMARY", "")
    break_threshold = float(os.environ.get("BREAK_THRESHOLD", "95.0"))

    print("==================================================")
    print("  Stryker.NET Release Quality Gate Validation")
    print("==================================================")
    print(f"Target Commit SHA: {target_sha}")
    print(f"Repository:        {repo}")
    print(f"Break Threshold:   {break_threshold}%")

    # 1. Try Commit Status API
    status_info = get_commit_status(repo, target_sha, token)

    # 2. Try Local Metadata if commit status was not available (e.g. running in same workspace or local test)
    local_info = check_local_metadata()

    # 3. Try Workflow Runs API
    workflow_info = get_workflow_run_status(repo, target_sha, token)

    analyzed_sha = target_sha
    analysis_date = "N/A"
    score = None
    passed = False
    source = "Unknown"
    status_text = "Unknown"

    if status_info:
        source = "GitHub Commit Status API (stryker/mutation-gate)"
        analysis_date = status_info.get("updated_at", "N/A")
        desc = status_info.get("description", "")
        status_state = status_info.get("state")
        score = parse_score_from_desc(desc)
        status_text = status_state

        if score is not None:
            passed = (score >= break_threshold)
        else:
            passed = (status_state == "success")
            score = 100.0 if passed else 0.0

    elif local_info:
        source = "Local mutation-summary.json artifact"
        analyzed_sha = local_info.get("commit_sha", target_sha)
        analysis_date = local_info.get("execution_date_utc", "N/A")
        score = float(local_info.get("mutation_score", 0.0))
        passed = (score >= break_threshold)
        status_text = local_info.get("status_badge", local_info.get("status", "N/A"))

    elif workflow_info:
        source = "GitHub Actions Stryker Workflow Run API"
        analyzed_sha = workflow_info.get("head_sha", target_sha)
        analysis_date = workflow_info.get("created_at", "N/A")
        conclusion = workflow_info.get("conclusion")
        status_text = conclusion
        passed = (conclusion == "success")
        score = 100.0 if passed else 0.0

    else:
        # No evidence found
        print(f"\n[ERROR] No valid Stryker mutation testing evidence found for commit {target_sha}!", file=sys.stderr)
        msg_md = f"""# 🚀 Release Quality Gate: Stryker Mutation Verification

### ❌ RELEASE BLOCKED

| Metric | Value |
|---|---|
| **Quality Gate** | ❌ **FAILED (NO DATA)** |
| **Target Commit** | `{target_sha}` |
| **Break Threshold** | `{break_threshold}%` |
| **Status** | No Stryker mutation test execution record found for this commit. |
| **Release Decision** | 🛑 **BLOCKED** |

> [!CAUTION]
> **Release Gate Violation:**
> Every release to NuGet must have a verified Stryker.NET mutation testing run with a score **>= {break_threshold}%**.
> Please trigger the `stryker.yml` workflow for this commit or merge via a verified `main` push.
"""
        if step_summary_path:
            with open(step_summary_path, "a", encoding="utf-8") as f:
                f.write(msg_md)
        else:
            print("\n" + msg_md)

        sys.exit(1)

    score_display = f"{score:.2f}%" if score is not None else "N/A"
    decision = "RELEASE ALLOWED" if passed else "RELEASE BLOCKED"
    decision_badge = "🟢 **RELEASE ALLOWED**" if passed else "🛑 **RELEASE BLOCKED**"

    print("\n--- Validation Audit Summary ---")
    print(f"Data Source:       {source}")
    print(f"Target Commit:     {target_sha}")
    print(f"Analyzed Commit:   {analyzed_sha}")
    print(f"Analysis Date:     {analysis_date}")
    print(f"Mutation Score:    {score_display}")
    print(f"Break Threshold:   {break_threshold}%")
    print(f"Quality Status:    {status_text}")
    print(f"Release Decision:  {decision}")

    summary_md = f"""# 🚀 Release Quality Gate: Stryker Mutation Verification

### {decision_badge}

| Metric | Value |
|---|---|
| **Quality Gate Status** | {'✅ **PASSED**' if passed else '❌ **FAILED**'} |
| **Target Commit** | `{target_sha}` |
| **Analyzed Commit** | `{analyzed_sha}` |
| **Mutation Score** | **`{score_display}`** |
| **Break Threshold** | `{break_threshold}%` |
| **Quality Tier** | {status_text} |
| **Execution Date** | {analysis_date} |
| **Evidence Source** | {source} |
| **Release Decision** | {decision_badge} |

"""

    if passed:
        summary_md += f"""
> [!NOTE]
> **Quality Gate Verified:**
> The mutation score (`{score_display}`) satisfies the minimum requirement (`>= {break_threshold}%`).
> Package build and NuGet publication are **permitted**.
"""
    else:
        summary_md += f"""
> [!CAUTION]
> **Quality Gate Breach:**
> The mutation score (`{score_display}`) is below the mandatory quality threshold (`{break_threshold}%`).
> Package build and NuGet publication are **strictly blocked**.
"""

    if step_summary_path:
        try:
            with open(step_summary_path, "a", encoding="utf-8") as f:
                f.write(summary_md)
        except Exception as e:
            print(f"[WARN] Could not write to Step Summary: {e}", file=sys.stderr)
    else:
        print("\n" + summary_md)

    if not passed:
        print(f"\n[ERROR] Quality gate failed. Mutation score {score_display} < {break_threshold}%.", file=sys.stderr)
        sys.exit(1)

    print("\n[SUCCESS] Stryker mutation quality gate verified. Proceeding with release.")
    sys.exit(0)


if __name__ == "__main__":
    main()
