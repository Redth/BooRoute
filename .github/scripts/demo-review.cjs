const fs = require("node:fs");

const model = "gpt-4.1-mini";
const bot = "github-actions[bot]";
const prefix = "<!-- booroute-demo-review:v1:";
const aiMarker = "<!-- booroute-demo-gpt -->";
const event = JSON.parse(fs.readFileSync(process.env.GITHUB_EVENT_PATH, "utf8"));
const number = Number(event.pull_request?.number || event.inputs?.pr_number);
const repository = process.env.GITHUB_REPOSITORY;
const token = process.env.GITHUB_TOKEN;
const budget = AbortSignal.timeout(50000);

if (!Number.isSafeInteger(number) || number < 1 ||
    !/^[A-Za-z0-9_.-]+\/[A-Za-z0-9_.-]+$/.test(repository) || !token) {
  throw new Error("A PR number, repository, and scoped GITHUB_TOKEN are required.");
}

async function api(path, method = "GET", body) {
  const response = await fetch(`https://api.github.com/repos/${repository}${path}`, {
    method,
    headers: {
      Authorization: `Bearer ${token}`,
      Accept: "application/vnd.github+json",
      "X-GitHub-Api-Version": "2022-11-28",
      "Content-Type": "application/json",
    },
    body: body === undefined ? undefined : JSON.stringify(body),
    signal: AbortSignal.any([budget, AbortSignal.timeout(10000)]),
  });
  if (!response.ok) throw new Error(`GitHub ${method} failed (${response.status}).`);
  return response.status === 204 ? undefined : response.json();
}

async function current(head, base) {
  const pr = await api(`/pulls/${number}`);
  const expected = event.pull_request;
  if (pr.state !== "open" ||
      (expected && (pr.head.sha !== expected.head.sha || pr.base.sha !== expected.base.sha)) ||
      (head && (pr.head.sha !== head || pr.base.sha !== base))) {
    console.log("Skipping a closed PR or stale revision.");
    return undefined;
  }
  return pr;
}

function anchor(file, id) {
  const patch = (file.patch || "").slice(0, 4000);
  let left = 0;
  let right = 0;
  let deletion;
  for (const line of patch.split("\n")) {
    const hunk = /^@@ -(\d+)(?:,\d+)? \+(\d+)(?:,\d+)? @@/.exec(line);
    if (hunk) {
      left = Number(hunk[1]);
      right = Number(hunk[2]);
    } else if (line.startsWith("+") && right > 0) {
      return { id, path: file.filename, line: right, side: "RIGHT", patch };
    } else if (line.startsWith("-") && left > 0) {
      deletion ||= { id, path: file.filename, line: left, side: "LEFT", patch };
      left++;
    } else if (line.startsWith(" ")) {
      left++;
      right++;
    }
  }
  return deletion || { id, path: file.filename, subject_type: "file", patch: "" };
}

async function candidates() {
  const files = await api(`/pulls/${number}/files?per_page=100`);
  return files.slice(0, 2).map((file, index) => anchor(file, index + 1));
}

async function comments() {
  const result = [];
  for (let page = 1; ; page++) {
    const batch = await api(`/pulls/${number}/comments?per_page=100&page=${page}`);
    result.push(...batch);
    if (batch.length < 100) return result;
  }
}

function marker(pr, id) {
  return `${prefix}${pr.head.sha}:${pr.base.sha}:${id} -->`;
}

function previous(all, pr, candidate) {
  return all.findLast(comment => comment.user?.login === bot &&
    comment.path === candidate.path && comment.body.includes(marker(pr, candidate.id)));
}

function fallback(candidate, reason) {
  const question = candidate.subject_type === "file"
    ? "Could a reviewer confirm the intent and coverage for this file change?"
    : candidate.side === "LEFT"
      ? "Does removing this code require corresponding updates to its callers or coverage?"
      : "Is this changed behavior covered for its relevant edge cases?";
  return {
    id: candidate.id,
    ai: false,
    body: `**Demo feedback - deterministic fallback, not an AI review.**\n\n${question}\n\n` +
      `GPT feedback is unavailable: ${reason}. No tests or runtime verification were performed.`,
  };
}

async function generate() {
  const pr = await current();
  if (!pr) return;
  const selected = await candidates();
  if (!selected.length) {
    console.warn("::warning::GitHub exposes no changed file to anchor inline demo feedback.");
    return;
  }
  const all = await comments();
  if (selected.every(candidate => previous(all, pr, candidate)?.body.includes(aiMarker))) {
    console.log("This revision already has GPT demo feedback; nothing to repeat.");
    return;
  }

  let feedback;
  try {
    const apiKey = process.env.OPENAI_API_KEY;
    if (!apiKey) throw new Error("repository secret OPENAI_API_KEY is not configured");
    const prompt = fs.readFileSync(process.env.GH_AW_PROMPT, "utf8");
    const response = await fetch("https://api.openai.com/v1/chat/completions", {
      method: "POST",
      headers: { Authorization: `Bearer ${apiKey}`, "Content-Type": "application/json" },
      body: JSON.stringify({
        model,
        messages: [
          { role: "system", content: prompt.slice(-20000) },
          { role: "user", content: JSON.stringify({ untrusted_diff_samples: selected }) },
        ],
        response_format: { type: "json_object" },
        temperature: 0.2,
        max_completion_tokens: 500,
      }),
      signal: AbortSignal.any([budget, AbortSignal.timeout(30000)]),
    });
    if (!response.ok) throw new Error(`OpenAI returned HTTP ${response.status}`);
    const completion = await response.json();
    const output = JSON.parse(completion.choices?.[0]?.message?.content);
    if (!Array.isArray(output.feedback) || output.feedback.length > 2) {
      throw new Error("GPT returned an invalid feedback shape");
    }
    feedback = selected.map(candidate => {
      const item = output.feedback.find(value => value.id === candidate.id);
      if (!item || !["question", "observation"].includes(item.kind) ||
          typeof item.body !== "string" || !item.body.trim() || item.body.length > 1200) {
        return fallback(candidate, "GPT returned no usable feedback for this location");
      }
      const text = item.body.replace(/<!--[\s\S]*?-->/g, "").replace(/@/g, "&#64;").trim();
      if (!text) return fallback(candidate, "GPT returned no usable text for this location");
      return {
        id: candidate.id,
        ai: true,
        body: `**Demo feedback - GPT ${item.kind}, not a verified finding.**\n\n${text}\n\n` +
          "Only a short diff excerpt was considered. No tests or runtime verification were performed.",
      };
    });
  } catch (error) {
    const reason = error.name === "TimeoutError" || error.name === "AbortError"
      ? "the single model request exceeded its time budget"
      : error.name === "SyntaxError" ? "GPT returned invalid JSON"
        : error.message.replace(/[\r\n]/g, " ").slice(0, 160);
    console.warn(`::warning::${reason}; using explicitly non-AI demo feedback.`);
    feedback = selected.map(candidate => fallback(candidate, reason));
  }
  const review = JSON.stringify({ number, head: pr.head.sha, base: pr.base.sha, feedback });
  fs.appendFileSync(process.env.GH_AW_SAFE_OUTPUTS,
    `${JSON.stringify({ type: "publish_demo_review", review })}\n`);
}

async function publish() {
  const output = JSON.parse(fs.readFileSync(process.env.GH_AW_AGENT_OUTPUT, "utf8"));
  const items = output.items.filter(item => item.type === "publish_demo_review");
  if (items.length !== 1) throw new Error("Expected exactly one demo review output.");
  const review = JSON.parse(items[0].review);
  if (review.number !== number || !/^[a-f0-9]{40}$/.test(review.head) ||
      !/^[a-f0-9]{40}$/.test(review.base) ||
      !Array.isArray(review.feedback) || review.feedback.length < 1 || review.feedback.length > 2) {
    throw new Error("Invalid demo review revision or feedback count.");
  }
  const pr = await current(review.head, review.base);
  if (!pr) return;
  const selected = await candidates();
  const all = await comments();
  const seen = new Set();
  const additions = [];
  for (const item of review.feedback) {
    const candidate = selected.find(value => value.id === item.id);
    if (!candidate || seen.has(item.id) || typeof item.ai !== "boolean" ||
        typeof item.body !== "string" || !item.body.startsWith("**Demo feedback - ") ||
        item.body.length > 2000) throw new Error("Invalid demo feedback item.");
    seen.add(item.id);
    const body = `${item.body}\n\n${marker(pr, item.id)}${item.ai ? `\n${aiMarker}` : ""}`;
    const existing = previous(all, pr, candidate);
    if (existing) {
      if (item.ai && !existing.body.includes(aiMarker)) {
        if (!await current(review.head, review.base)) return;
        if (process.env.GH_AW_SAFE_OUTPUTS_STAGED !== "true") {
          await api(`/pulls/comments/${existing.id}`, "PATCH", { body });
        }
      }
    } else {
      const { patch, id, ...location } = candidate;
      additions.push({ ...location, body });
    }
  }
  if (process.env.GH_AW_SAFE_OUTPUTS_STAGED === "true") {
    console.log("Staged demo feedback; no GitHub writes.");
    return;
  }
  const lines = additions.filter(item => item.subject_type !== "file");
  if (lines.length && await current(review.head, review.base)) {
    await api(`/pulls/${number}/reviews`, "POST", {
      commit_id: review.head,
      event: "COMMENT",
      body: "Demo feedback only, not approval, a full code review, or a verification result.",
      comments: lines,
    });
  }
  for (const file of additions.filter(item => item.subject_type === "file")) {
    if (!await current(review.head, review.base)) return;
    await api(`/pulls/${number}/comments`, "POST", { ...file, commit_id: review.head });
  }
  console.log(`Reconciled up to ${review.feedback.length} inline demo comments on PR ${number}.`);
}

const mode = process.argv[2];
(mode === "generate" ? generate() : mode === "publish" ? publish() :
  Promise.reject(new Error("Expected generate or publish mode.")))
  .catch(error => {
    console.error(`::error::${error.message}`);
    process.exitCode = 1;
  });
