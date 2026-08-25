// Copyright © Erickson Lopez. MIT License.
const fs = require('fs');
const path = require('path');

/**
 * Loads thresholds dynamically from stryker-config.json (Single Source of Truth).
 * @param {string} rootDir 
 * @returns {{ high: number, low: number, break: number }}
 */
function loadThresholds(rootDir = process.cwd()) {
  let thresholds = { high: 100, low: 98, break: 95 };
  try {
    const configPath = path.join(rootDir, 'stryker-config.json');
    if (fs.existsSync(configPath)) {
      const config = JSON.parse(fs.readFileSync(configPath, 'utf8'));
      const t = config['stryker-config']?.thresholds || config.thresholds || {};
      thresholds = {
        high: Number(t.high ?? 100),
        low: Number(t.low ?? 98),
        break: Number(t.break ?? 95),
      };
    }
  } catch (err) {
    console.warn(`[WARN] Could not parse stryker-config.json: ${err.message}`);
  }
  return thresholds;
}

/**
 * Finds all candidate JSON report files under a given directory.
 * @param {string} dir 
 * @returns {string[]}
 */
function findJsonReports(dir) {
  let results = [];
  if (!fs.existsSync(dir)) return results;
  const entries = fs.readdirSync(dir, { withFileTypes: true });
  for (const entry of entries) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      results = results.concat(findJsonReports(full));
    } else if (
      entry.name.endsWith('.json') &&
      !entry.name.endsWith('.html.json') &&
      !entry.name.endsWith('metadata.json') &&
      !entry.name.endsWith('stryker-metadata.json')
    ) {
      results.push(full);
    }
  }
  return results;
}

/**
 * Evaluates a mutation score against configured thresholds.
 * @param {number} score 
 * @param {{ high: number, low: number, break: number }} thresholds 
 * @returns {{ statusLabel: string, passedGate: boolean }}
 */
function evaluateMutationScore(score, thresholds) {
  const passedGate = score >= thresholds.break;
  let statusLabel = '❌ FAILED';
  if (score >= thresholds.high) {
    statusLabel = '✅ HIGH';
  } else if (score >= thresholds.low) {
    statusLabel = '🟡 LOW';
  } else if (score >= thresholds.break) {
    statusLabel = '🟠 WARNING';
  }
  return { statusLabel, passedGate };
}

function main() {
  const thresholds = loadThresholds();
  let score = 0;
  let killed = 0;
  let total = 0;
  let foundReport = false;
  let parsedFile = null;

  const jsonFiles = findJsonReports('StrykerOutput');

  // Prioritize mutation-report.json first, then mutation-summary.json, then others
  jsonFiles.sort((a, b) => {
    const aBase = path.basename(a).toLowerCase();
    const bBase = path.basename(b).toLowerCase();
    if (aBase === 'mutation-report.json') return -1;
    if (bBase === 'mutation-report.json') return 1;
    if (aBase === 'mutation-summary.json') return -1;
    if (bBase === 'mutation-summary.json') return 1;
    return 0;
  });

  for (const jsonPath of jsonFiles) {
    try {
      const data = JSON.parse(fs.readFileSync(jsonPath, 'utf8'));

      // Check standard Stryker report format (camelCase or snake_case)
      if (data.mutationScore !== undefined) {
        score = Number(data.mutationScore);
      } else if (data.mutation_score !== undefined) {
        score = Number(data.mutation_score);
      }

      // Count mutants from files map if present
      if (data.files && typeof data.files === 'object') {
        let fKilled = 0;
        let fTotal = 0;
        for (const f of Object.values(data.files)) {
          for (const m of (f.mutants || [])) {
            const st = String(m.status || '').toLowerCase();
            if (st === 'killed' || st === 'timeout') {
              fKilled++;
              fTotal++;
            } else if (st === 'survived' || st === 'nocoverage') {
              fTotal++;
            }
          }
        }
        if (fTotal > 0) {
          killed = fKilled;
          total = fTotal;
        }
      }

      // If killed/total not populated from files, check summary properties
      if (total === 0) {
        if (data.mutants_killed !== undefined) {
          killed = Number(data.mutants_killed);
        }
        if (data.total_mutants !== undefined) {
          total = Number(data.total_mutants);
        } else if (data.valid_mutants !== undefined) {
          total = Number(data.valid_mutants);
        }
      }

      if (total > 0 && score === 0 && data.mutationScore === undefined && data.mutation_score === undefined) {
        score = Math.round((killed / total) * 10000) / 100;
      }

      foundReport = true;
      parsedFile = jsonPath;
      break; // Successfully parsed primary report
    } catch (err) {
      console.warn(`[WARN] Error parsing ${jsonPath}: ${err.message}`);
    }
  }

  const { statusLabel, passedGate } = evaluateMutationScore(score, thresholds);
  const executionDate = new Date().toISOString();
  const sha = process.env.GITHUB_SHA || 'unknown';
  const repo = process.env.GITHUB_REPOSITORY || '';
  const runId = process.env.GITHUB_RUN_ID || '';
  const serverUrl = process.env.GITHUB_SERVER_URL || 'https://github.com';
  const runUrl = repo && runId ? `${serverUrl}/${repo}/actions/runs/${runId}` : '';

  // Save structured metadata artifact
  const metadata = {
    commit_sha: sha,
    execution_date: executionDate,
    mutation_score: score,
    mutants_killed: killed,
    total_mutants: total,
    threshold_high: thresholds.high,
    threshold_low: thresholds.low,
    threshold_break: thresholds.break,
    status: statusLabel,
    passed: passedGate && foundReport,
    run_url: runUrl,
    report_source: parsedFile || 'none'
  };
  fs.writeFileSync('stryker-metadata.json', JSON.stringify(metadata, null, 2));

  // Write GitHub Step Summary
  const stepSummaryPath = process.env.GITHUB_STEP_SUMMARY;
  if (stepSummaryPath) {
    const summary = `
## 🛡️ Stryker Mutation Testing Results

| Metric | Value |
|--------|-------|
| **Mutation Score** | **${score}%** |
| **Mutants Killed** | ${killed} |
| **Total Mutants** | ${total} |
| **Threshold High** | $\\ge ${thresholds.high}\\%$ |
| **Threshold Low** | $\\ge ${thresholds.low}\\%$ |
| **Threshold Break** | $\\ge ${thresholds.break}\\%$ |
| **Status** | ${statusLabel} |
| **Commit SHA** | \`${sha.substring(0, 7)}\` (\`${sha}\`) |
| **Execution Date** | ${executionDate} |

${passedGate && foundReport
  ? `> [!TIP]\n> Stryker mutation testing passed the quality gate (Score: **${score}%** $\\ge$ Break: **${thresholds.break}%**).`
  : `> [!CAUTION]\n> Stryker mutation score (**${score}%**) is below the break threshold (**${thresholds.break}%**). Quality gate failed.`}
`;
    fs.appendFileSync(stepSummaryPath, summary);
  }

  // Set GitHub Action Outputs
  const outputPath = process.env.GITHUB_OUTPUT;
  if (outputPath) {
    fs.appendFileSync(outputPath, `score=${score}\n`);
    fs.appendFileSync(outputPath, `passed_gate=${passedGate && foundReport}\n`);
    fs.appendFileSync(outputPath, `status=${statusLabel}\n`);
    fs.appendFileSync(outputPath, `killed=${killed}\n`);
    fs.appendFileSync(outputPath, `total=${total}\n`);
    fs.appendFileSync(outputPath, `threshold_high=${thresholds.high}\n`);
    fs.appendFileSync(outputPath, `threshold_low=${thresholds.low}\n`);
    fs.appendFileSync(outputPath, `threshold_break=${thresholds.break}\n`);
    fs.appendFileSync(outputPath, `execution_date=${executionDate}\n`);
    fs.appendFileSync(outputPath, `commit_sha=${sha}\n`);
  }

  console.log(`============================================================`);
  console.log(`  STRYKER MUTATION TESTING RESULT`);
  console.log(`============================================================`);
  console.log(`Mutation Score : ${score}%`);
  console.log(`Mutants Killed : ${killed} / ${total}`);
  console.log(`Thresholds     : High: ≥${thresholds.high}%, Low: ≥${thresholds.low}%, Break: ≥${thresholds.break}%`);
  console.log(`Status         : ${statusLabel}`);
  console.log(`Quality Gate   : ${passedGate && foundReport ? 'PASSED' : 'FAILED'}`);
  console.log(`Commit SHA     : ${sha}`);
  console.log(`Execution Date : ${executionDate}`);
  console.log(`Report File    : ${parsedFile || 'None'}`);
  console.log(`============================================================`);

  return { score, killed, total, statusLabel, passed: passedGate && foundReport };
}

if (require.main === module) {
  main();
}

module.exports = {
  main,
  loadThresholds,
  findJsonReports,
  evaluateMutationScore,
};
