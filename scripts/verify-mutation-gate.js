// Copyright © Erickson Lopez. MIT License.
const fs = require('fs');
const path = require('path');

/**
 * Loads threshold configuration dynamically from stryker-config.json (Single Source of Truth).
 * @param {string} rootDir 
 * @returns {{ high: number, low: number, break: number }}
 */
function loadThresholds(rootDir = process.cwd()) {
  try {
    const configPath = path.join(rootDir, 'stryker-config.json');
    if (fs.existsSync(configPath)) {
      const config = JSON.parse(fs.readFileSync(configPath, 'utf8'));
      const thresholds = config['stryker-config']?.thresholds || config.thresholds || {};
      return {
        high: Number(thresholds.high ?? 100),
        low: Number(thresholds.low ?? 98),
        break: Number(thresholds.break ?? 95),
      };
    }
  } catch (err) {
    console.warn(`[WARN] Could not load stryker-config.json, using defaults (100/98/95): ${err.message}`);
  }
  return { high: 100, low: 98, break: 95 };
}

/**
 * Parses mutation score from status description string (e.g. "Stryker: 99.96% (2310/6714 killed) - 🟡 LOW").
 * @param {string} description 
 * @returns {number|null}
 */
function parseScoreFromDescription(description) {
  if (!description) return null;
  const match = /(\d+(?:\.\d+)?)\s*%/.exec(description);
  return match ? Number.parseFloat(match[1]) : null;
}

/**
 * Evaluates a mutation score against configured thresholds.
 * Policy:
 *   >= 100%          -> ✅ HIGH
 *   >= 98% && < 100% -> 🟡 LOW
 *   >= 95% && < 98%  -> 🟠 WARNING
 *   < 95%            -> ❌ FAILED
 * @param {number} score 
 * @param {{ high: number, low: number, break: number }} thresholds 
 * @returns {{ status: string, passedBreak: boolean }}
 */
function evaluateScore(score, thresholds) {
  const passedBreak = score >= thresholds.break;
  let status = '❌ FAILED';
  if (score >= thresholds.high) {
    status = '✅ HIGH';
  } else if (score >= thresholds.low) {
    status = '🟡 LOW';
  } else if (score >= thresholds.break) {
    status = '🟠 WARNING';
  }
  return { status, passedBreak };
}

/**
 * Main verification function invoked from GitHub Actions or CLI.
 * Answers the 5 required release verification questions:
 *   1. Which commit was evaluated?
 *   2. When?
 *   3. What mutation score was obtained?
 *   4. Did it pass the break threshold?
 *   5. Can the release proceed?
 *
 * @param {{ github?: any, context?: any, core?: any }} params 
 */
async function verifyMutationGate({ github, context, core } = {}) {
  const thresholds = loadThresholds();
  const owner = context?.repo?.owner || process.env.GITHUB_REPOSITORY_OWNER || 'ericksonlopezf';
  const repo = context?.repo?.repo || (process.env.GITHUB_REPOSITORY ? process.env.GITHUB_REPOSITORY.split('/')[1] : 'dotnet-pagination');
  const targetSha = process.env.TARGET_SHA || context?.sha || 'unknown';

  console.log(`============================================================`);
  console.log(`  STRYKER MUTATION TESTING RELEASE GATE VERIFIER`);
  console.log(`============================================================`);
  console.log(`Repository      : ${owner}/${repo}`);
  console.log(`Target Commit   : ${targetSha}`);
  console.log(`Thresholds      : High: ≥${thresholds.high}%, Low: ≥${thresholds.low}%, Break: ≥${thresholds.break}%`);
  console.log(`============================================================\n`);

  let evaluatedCommit = null;
  let executionDate = null;
  let mutationScore = null;
  let statusState = null;
  let statusDescription = null;
  let runUrl = null;
  let evaluationSource = null;

  // ── Strategy 1: Check commit status on targetSha via GitHub API ─────────────
  if (github && targetSha !== 'unknown') {
    try {
      const statusesResp = await github.rest.repos.getCombinedStatusForRef({
        owner,
        repo,
        ref: targetSha,
      });

      const strykerStatus = (statusesResp.data.statuses || []).find(
        s => s.context === 'mutation-testing/stryker' || s.context === 'stryker/mutation-score' || s.context === 'stryker/mutation-gate'
      );

      if (strykerStatus) {
        evaluatedCommit = targetSha;
        statusState = strykerStatus.state;
        statusDescription = strykerStatus.description;
        executionDate = strykerStatus.updated_at || strykerStatus.created_at;
        runUrl = strykerStatus.target_url;
        mutationScore = parseScoreFromDescription(statusDescription);
        evaluationSource = 'commit_status (target commit)';
      }
    } catch (err) {
      console.log(`[INFO] No direct commit status found on target commit: ${err.message}`);
    }
  }

  // ── Strategy 2: Check recent commits on main (up to 15 commits back) ────────
  // When Release Please merges a release PR, the release commit only updates version/changelog.
  // The mutation testing result on main belongs to the latest code commit.
  if (!evaluatedCommit && github) {
    console.log(`[INFO] Searching recent commits on 'main' for Stryker mutation status...`);
    try {
      const commitsResp = await github.rest.repos.listCommits({
        owner,
        repo,
        sha: 'main',
        per_page: 15,
      });

      for (const commitObj of (commitsResp.data || [])) {
        const cSha = commitObj.sha;
        const cStatusResp = await github.rest.repos.getCombinedStatusForRef({
          owner,
          repo,
          ref: cSha,
        });

        const sStatus = (cStatusResp.data.statuses || []).find(
          s => s.context === 'mutation-testing/stryker' || s.context === 'stryker/mutation-score' || s.context === 'stryker/mutation-gate'
        );

        if (sStatus) {
          evaluatedCommit = cSha;
          statusState = sStatus.state;
          statusDescription = sStatus.description;
          executionDate = sStatus.updated_at || sStatus.created_at || commitObj.commit.committer?.date;
          runUrl = sStatus.target_url;
          mutationScore = parseScoreFromDescription(statusDescription);
          evaluationSource = `commit_status (${cSha.substring(0, 7)})`;
          break;
        }
      }
    } catch (err) {
      console.log(`[INFO] Could not search commit history: ${err.message}`);
    }
  }

  // ── Strategy 3: Check completed workflow runs for mutation-testing.yml on main
  if (!evaluatedCommit && github) {
    console.log(`[INFO] Searching completed workflow runs for 'mutation-testing.yml' on main...`);
    try {
      const runsResp = await github.rest.actions.listWorkflowRuns({
        owner,
        repo,
        workflow_id: 'mutation-testing.yml',
        branch: 'main',
        status: 'completed',
        per_page: 5,
      });

      const runs = runsResp.data?.workflow_runs || [];
      if (runs.length > 0) {
        const latestRun = runs[0];
        evaluatedCommit = latestRun.head_sha;
        statusState = latestRun.conclusion === 'success' ? 'success' : 'failure';
        executionDate = latestRun.updated_at || latestRun.created_at;
        runUrl = latestRun.html_url;
        evaluationSource = `workflow_run (${latestRun.id})`;

        if (latestRun.conclusion === 'success') {
          mutationScore = 100.0;
        } else {
          mutationScore = 0.0;
        }
      }
    } catch (err) {
      console.log(`[INFO] Could not fetch workflow runs: ${err.message}`);
    }
  }

  // ── Strategy 4: Local fallback (metadata file) ──────────────────────────────
  if (!evaluatedCommit && fs.existsSync('stryker-metadata.json')) {
    try {
      const meta = JSON.parse(fs.readFileSync('stryker-metadata.json', 'utf8'));
      evaluatedCommit = meta.commit_sha || targetSha;
      executionDate = meta.execution_date;
      mutationScore = Number(meta.mutation_score);
      statusState = meta.passed ? 'success' : 'failure';
      runUrl = meta.run_url;
      evaluationSource = 'local stryker-metadata.json';
    } catch (err) {
      console.log(`[INFO] Could not read local stryker-metadata.json: ${err.message}`);
    }
  }

  // ── Evaluation & Decision ──────────────────────────────────────────────────
  if (!evaluatedCommit) {
    const errorMsg = `❌ STRYKER GATE FAILED: No valid Stryker mutation testing result found for 'main'. A passing mutation test run (score ≥ ${thresholds.break}%) is required before releasing.`;
    console.error(errorMsg);

    const summary = `
## 🛡️ Stryker Mutation Quality Gate (Release Validation)

| Audit Item | Value |
|---|---|
| **Target Release Commit** | \`${targetSha.substring(0, 7)}\` |
| **Evaluated Commit** | *None found on main* |
| **Execution Date** | *N/A* |
| **Mutation Score** | *N/A* |
| **Break Threshold** | $\\ge ${thresholds.break}\\%$ |
| **Quality Gate Status** | ❌ **BLOCKED (No mutation test evidence found on main)** |
| **Release Permitted** | **NO** |

> [!CAUTION]
> No passing Stryker mutation testing run was identified for \`main\`.
> Ensure \`mutation-testing.yml\` has run successfully on \`main\` before releasing.
`;
    if (core && core.summary) {
      await core.summary.addRaw(summary).write();
    }
    if (core && typeof core.setFailed === 'function') {
      core.setFailed(errorMsg);
    }
    return { passed: false, reason: 'No mutation test evidence found on main' };
  }

  const scoreValue = mutationScore !== null ? mutationScore : (statusState === 'success' ? 100.0 : 0.0);
  const evaluation = evaluateScore(scoreValue, thresholds);
  const isStateSuccess = statusState === 'success';
  const passedBreak = evaluation.passedBreak && isStateSuccess;
  const status = isStateSuccess ? evaluation.status : '❌ FAILED';

  console.log(`------------------------------------------------------------`);
  console.log(`  RELEASE GATE VERIFICATION QUESTIONS & ANSWERS`);
  console.log(`------------------------------------------------------------`);
  console.log(`1. Which commit was evaluated?       : ${evaluatedCommit} (${evaluationSource})`);
  console.log(`2. When?                             : ${executionDate || 'N/A'}`);
  console.log(`3. What mutation score was achieved? : ${scoreValue}% (${status})`);
  console.log(`4. Did it pass the break threshold?  : ${passedBreak ? 'YES (>= ' + thresholds.break + '%)' : 'NO (< ' + thresholds.break + '%)'}`);
  console.log(`5. Can the release proceed?          : ${passedBreak ? 'YES (ALLOWED)' : 'NO (BLOCKED)'}`);
  console.log(`------------------------------------------------------------\n`);

  if (core && typeof core.setOutput === 'function') {
    core.setOutput('evaluated_commit', evaluatedCommit);
    core.setOutput('execution_date', executionDate || '');
    core.setOutput('mutation_score', String(scoreValue));
    core.setOutput('passed_break_gate', String(passedBreak));
    core.setOutput('can_proceed', String(passedBreak));
    core.setOutput('status', status);
  }

  // Write markdown Step Summary for GitHub Actions
  if (core && core.summary) {
    const summary = `
## 🛡️ Stryker Mutation Quality Gate (Release Validation)

| Audit Item | Value |
|---|---|
| **Evaluated Commit SHA** | \`${evaluatedCommit.substring(0, 7)}\` (\`${evaluatedCommit}\`) |
| **Target Release Commit** | \`${targetSha.substring(0, 7)}\` (\`${targetSha}\`) |
| **Execution Date** | ${executionDate || 'N/A'} |
| **Mutation Score** | **${scoreValue}%** |
| **Break Threshold** | $\\ge ${thresholds.break}\\%$ (Low: $\\ge ${thresholds.low}\\%$, High: $\\ge ${thresholds.high}\\%$) |
| **Threshold Status** | ${status} |
| **Passed Break Threshold** | ${passedBreak ? '✅ **YES**' : '❌ **NO**'} |
| **Evidence Source** | \`${evaluationSource}\` |
| **Release Permitted** | ${passedBreak ? '✅ **YES (Release ALLOWED)**' : '❌ **NO (Release BLOCKED)**'} |

${runUrl ? `[View Stryker Workflow Run](${runUrl})\n` : ''}
${passedBreak
  ? `> [!TIP]\n> Verified Stryker mutation testing quality gate passed (Score: **${scoreValue}%** $\\ge$ Break: **${thresholds.break}%**). Release is permitted.`
  : `> [!CAUTION]\n> Stryker mutation score (**${scoreValue}%**) is below the **${thresholds.break}%** break threshold. Release is blocked.`}
`;
    await core.summary.addRaw(summary).write();
  }

  if (!passedBreak) {
    const failMsg = `❌ STRYKER GATE FAILED: Mutation score (${scoreValue}%) is below break threshold (${thresholds.break}%). Release is blocked.`;
    console.error(failMsg);
    if (core && typeof core.setFailed === 'function') {
      core.setFailed(failMsg);
    }
    return { passed: false, reason: failMsg };
  }

  console.log(`✅ STRYKER MUTATION TESTING RELEASE GATE PASSED: Release permitted.`);
  return {
    passed: true,
    evaluatedCommit,
    executionDate,
    mutationScore: scoreValue,
    passedBreak,
    status
  };
}

if (require.main === module) {
  verifyMutationGate();
}

module.exports = verifyMutationGate;
module.exports.verifyMutationGate = verifyMutationGate;
module.exports.loadThresholds = loadThresholds;
module.exports.parseScoreFromDescription = parseScoreFromDescription;
module.exports.evaluateScore = evaluateScore;
