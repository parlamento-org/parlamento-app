# Parliament App Product Flow Guide

This document captures product and UX guidance that emerged while analyzing the scraper samples and planning the importer for `parlamento-app`. It is intentionally separate from [SCRAPER_MIGRATION_SPEC.md](C:/Users/Poodlers/OneDrive/Desktop/Pwogwamin/parlamento-scraper/SCRAPER_MIGRATION_SPEC.md), which is the technical importer/database handoff.

The app idea is a "Tinder-like" experience for Portuguese Parliament initiatives: users see anonymized proposal content, react on the merits, then can compare their instinct with Parliament's actual votes and inspect official sources.

## Core Product Thesis

The strongest version of the app is not "users vote on laws." That would be too vague and sometimes inaccurate.

The stronger, more honest framing is:

> Users react to an introduced parliamentary initiative in principle, before seeing who proposed it, then compare their reaction with the parliamentary generality vote and explore the official debate/context if they choose.

This distinction matters because Portuguese parliamentary initiatives are not all the same thing:

| Initiative type | Product meaning |
| --- | --- |
| `Projeto de Lei` | A bill introduced by parliamentary groups/deputies. It can become law after later phases. |
| `Proposta de Lei` | A government bill/proposal. It can also become law after later phases. |
| `Projeto de Resolução` | A resolution/recommendation. It can be voted on, but should not be presented as a binding law. |

The frontend should always display the initiative type. Avoid using a single generic label like "law" for every card.

## Recommended User Vote Target

Default user votes should map to the parliamentary `250` phase: `Votação na generalidade`.

Reasoning:

- Generality is closest to "do you support this idea in principle?"
- The app is showing the introduced proposal text, not necessarily the final text.
- Speciality and final-global stages can alter the proposal.
- Final global votes can refer to combined final texts involving multiple initiatives.
- Published laws can differ materially from the original introduced proposal.

Recommended wording:

```text
Would you support this initiative in principle, based on the introduced text?
```

Avoid:

```text
Would you pass this law?
```

That wording is only safe if the card is explicitly showing the final/published law text.

## Card Flow

Recommended default flow:

1. Show an anonymized card.
2. Ask the user for a simple reaction.
3. Record the user vote.
4. Reveal authorship and parliamentary outcome.
5. Offer deeper context through official links, debate videos, and vote details.

### Before Vote

Show:

- Initiative type, e.g. `Projeto de Lei`, `Proposta de Lei`, `Projeto de Resolução`.
- Neutral title, if the title itself does not reveal authorship.
- AI summary if available.
- Redacted/anonymized introduced text or selected excerpts.
- A "read official text" link if possible, though this may reveal identity/source metadata.

Hide initially:

- proposing party/group
- deputy names
- government author identity where feasible
- party vote positions
- final parliamentary result

The point is not to permanently hide politics. The point is to reduce first-click partisan reflexes.

### Vote Actions

Keep actions simple:

- Support in principle
- Oppose in principle
- Need more info / Skip

If the UI is swipe-based:

- right = support
- left = oppose
- up/details button = learn more
- neutral/skip button should exist, because many proposals cannot be judged responsibly from a short card

Do not force a binary decision for complex legal texts.

### Skip Is Not Abstention

The app should distinguish:

- user supports the initiative in principle
- user opposes the initiative in principle
- user skips because they are not interested, not informed enough, or do not want to vote
- parliamentary abstention, which is an official vote orientation by MPs/parties

Do not label a user skip as "abstain." In the app, "skip" is an engagement/ranking signal, not a political position.

Store skip events in the backend because they are useful for feed quality, but keep them separate from user votes in analytics.

### After Vote Reveal

After the user's vote, reveal:

- proposer type: government, parliamentary group, deputies, committee, other
- proposing party/group/deputies when available
- actual generality vote result
- party vote blocks: in favor, against, abstention, absence when parsed
- final/global result if available, clearly labelled as a later stage
- whether the initiative became a law, decree, resolution, or remained pending
- official source text link
- debate transcript links (`URLDiario`)
- debate video links

This creates a fair exchange: the user gets a low-bias first impression, then receives political accountability and source context.

## Avoiding Misleading Comparisons

The app should be careful when comparing user votes to Parliament.

Safe comparison:

```text
You supported this in principle. In Parliament's generality vote, it was approved.
```

Risky comparison:

```text
You agreed with Party X.
```

That may be true in one vote stage, but parties may support in generality and oppose final text, abstain for procedural reasons, or vote differently after amendments.

Recommended comparison model:

- Primary comparison: user vote vs `250` generality vote.
- Secondary context: final global vote (`320`) if available.
- Later legal status: decree/promulgation/publication if available.
- Always label stages clearly.

Example display:

```text
Your vote: Support in principle
Parliament, generality vote: Approved
Final global vote: Approved
Current status: Published as law
```

For `Projeto de Resolução`:

```text
Current status: Resolution/recommendation approved
```

Not:

```text
Published as law
```

## Speciality and Final Text

Speciality votes and final/global votes should generally not be part of the main swipe loop.

Reasons:

- The user is usually seeing the introduced proposal text.
- Speciality can alter details substantially.
- Some initiatives have no `310` speciality vote in the observed samples.
- The final/global vote can refer to a combined final text involving multiple initiatives.
- Asking users to vote again on speciality/final text would likely slow the product down and weaken the simple swipe experience.

Recommended treatment:

- Main card: user votes on the introduced initiative in principle.
- Primary comparison: `250` generality vote.
- Post-vote/detail view: show speciality vote (`310`) when available.
- Post-vote/detail view: show final/global vote (`320`) when available.
- Lifecycle view: show later phases such as decree, promulgation, referenda, INCM dispatch, and law publication.
- If final text documents are available through `TextosAprovados`, `OevTextId`, or event attachments, store/link them as official downstream texts.

Do not hide speciality/final votes. They are important context. Just do not make them the default user interaction unless the app later introduces a separate "advanced review" mode.

Possible future advanced mode:

```text
This proposal changed after the generality vote. Review the final text?
```

That should be optional and outside the fast swipe flow.

## Proposal Feed Ranking

The feed needs a simple way to avoid serving users too many boring or irrelevant proposals. The idea of increasing a proposal score when users vote and decreasing it when users skip is directionally right, but a raw `interestScore += 1` / `interestScore -= 1` can become unfair over time.

Problems with a raw score:

- New proposals start with no data and may never catch up.
- Controversial proposals may dominate even if they are low quality.
- Niche proposals can get buried after a few skips.
- Older proposals accumulate more interactions simply because they have been around longer.
- A proposal shown to the wrong audience may be penalized even if another audience would care.

Recommended first version:

Store immutable interaction events and derive raw counters from them. Useful event types:

- `Impression`
- `Support`
- `Oppose`
- `Skip`
- `DetailOpen`
- `SourceLinkClick`
- `DebateLinkClick`
- `PostVoteReveal`

Then maintain aggregate counters for fast ranking:

- `impressions`
- `support_votes`
- `oppose_votes`
- `skips`
- `detail_opens`
- `source_link_clicks`
- `debate_link_clicks`
- `post_vote_reveals`

Then compute a ranking score from rates, not only raw counts.

Suggested simple score:

```text
engagementRate = (support_votes + oppose_votes + 0.5 * detail_opens + 0.75 * source_link_clicks + 0.75 * debate_link_clicks)
                 / max(1, impressions)

skipRate = skips / max(1, impressions)

interestScore = engagementRate - 0.6 * skipRate
```

Then add guardrails:

- give new proposals an exploration boost until they reach a minimum number of impressions
- apply time decay so very old proposals do not dominate forever
- avoid showing the same proposal repeatedly to the same user
- optionally diversify by initiative type/topic/party/time period

Better simple formula:

```text
score = bayesianEngagementEstimate
      + freshnessBoost
      + explorationBoost
      - skipPenalty
```

Where:

- `bayesianEngagementEstimate` prevents tiny samples from overreacting.
- `freshnessBoost` helps recent/current proposals appear.
- `explorationBoost` gives under-sampled proposals a chance.
- `skipPenalty` lowers proposals that many users skip after seeing them.

For the first implementation, it is fine to start simple:

```text
interestScore = support_votes + oppose_votes - skips
```

But still store the raw counters separately so the algorithm can evolve without losing information.

Important: support and oppose should both increase interest. The ranking question is "was this engaging enough to vote on?", not "was it popular?"

### Feed Serving Rules

Recommended serving flow:

1. Exclude proposals the user already voted on or skipped recently.
2. Prefer proposals with available introduced text or summary.
3. Mix high-interest proposals with exploration candidates.
4. Avoid long streaks of the same initiative type or topic.
5. Keep some randomness so the app does not feel deterministic or politically narrow.

Example blend:

```text
70% ranked by interest score
20% under-sampled/new proposals
10% random eligible proposals
```

This keeps the Tinder-like feeling while still learning what users actually engage with.

### User-Level Personalization Later

Do not overbuild personalization at first. Later, the app can learn:

- initiative types the user tends to engage with
- topics inferred from summaries/text
- whether the user prefers current or historical proposals
- whether the user often opens debate/source links

But avoid creating a pure political comfort bubble. Civic products should keep some serendipity.

## Anonymization and Redaction

The anonymized pre-vote view is a good idea, but it must be implemented carefully.

Redact:

- party names
- party acronyms
- deputy names
- obvious author labels
- headings that reveal the proposer when feasible

Do not blindly redact:

- names of laws being amended
- public institutions
- ministries or agencies if they are the subject of the proposal
- regions, municipalities, courts, public bodies, or legal concepts

Bad redaction can remove essential meaning. The redaction service should have a policy version and should be testable on known examples.

Recommended UI language:

```text
Identity cues are hidden until you vote.
```

Do not imply the redacted text is the official text. It is a transformed reading view.

## AI Summaries

AI summaries can be valuable because official proposal texts are often long and legalistic. They should be treated as optional product aids, not as canonical content.

Product rules:

- The app must work without summaries.
- Summary generation should be optional, deferred, and budget-controlled.
- Summaries should be generated from extracted/redacted official text.
- A card can show "summary unavailable" and still be usable.
- Summaries should be labelled clearly.

Recommended label:

```text
AI-generated summary from the official proposal text
```

Recommended summary structure:

- What it proposes
- Who/what it affects
- Main practical change
- Caveats or unknowns, if the text is unclear

Avoid:

- persuasive framing
- predicting impacts not stated in the proposal
- saying something is good/bad policy
- presenting the summary as complete legal analysis

Good summary behavior:

- cite that it is based on the introduced text
- link to official text
- regenerate when source text, redaction policy, prompt, or model changes
- store model/prompt/input hash for audit
- allow users to report misleading summaries

Ethical position: AI summaries are acceptable when they reduce access friction and preserve auditability. They become unethical if they replace official context, hide uncertainty, or steer users toward a conclusion.

## Debate and Context Flow

The app should not try to show the whole debate on the main swipe card. That would ruin the product. But it should make debate accessible.

Use a progressive disclosure model:

1. Card: short redacted summary/text.
2. Details: proposal text, type, dates, stage, official vote.
3. Context: debate transcript links and video clips.
4. Deep source: official Diário/transcript pages and original document links.

Store and expose:

- `PublicacaoFase[].URLDiario`
- speaker-level publication links
- `Intervencoesdebates[].oradores[].linkVideo[]`
- debate `sumario`
- speaker names/party only after reveal or in details mode

Important caveat: debate summaries can refer to several initiatives discussed together. The UI should not imply every sentence in a debate summary applies only to the current card.

## Suggested Screens

### Swipe Deck

Purpose: quick first reactions.

Should show:

- type badge
- short title or neutral generated title
- AI summary if available
- redacted text excerpt
- support/oppose/skip controls
- details button

Should not show pre-vote:

- author party
- party vote results
- final result

### Post-Vote Reveal

Purpose: accountability and learning.

Should show:

- user's vote
- actual generality result
- party vote breakdown
- proposer identity
- final status if available
- speciality/final-global votes as later lifecycle context, not as a second required user vote
- "where parties stood" section
- "official sources" section

### Initiative Detail

Purpose: source-rich exploration.

Should show:

- full metadata
- initiative lifecycle timeline
- introduced text
- redacted text toggle if useful
- AI summary with disclaimer
- all votes by stage
- debate videos/transcripts
- official documents and attachments
- final text/published law status if available

### History/Profile

Purpose: user reflection.

Could show:

- user's votes over time
- user's skips separately from votes
- agreement with Parliament overall
- agreement with parties by generality votes
- topics/types the user supports/opposes

Be careful: "agreement with parties" should be explained as approximate and stage-specific.

## Tone and Trust

The app is playful, but it is dealing with civic information. The tone should be clear, humble, and transparent.

Good tone:

```text
This is the introduced version. Later amendments may have changed it.
```

```text
You are voting on the idea in principle.
```

```text
This summary is generated. Check the official text for full detail.
```

Avoid:

```text
This party betrayed you.
```

```text
You voted for the law.
```

```text
AI says this proposal means...
```

The app should help people think, not dunk on them or trick them into shallow certainty.

## Product Guardrails

Must-have guardrails:

- Label initiative type.
- Label vote stage.
- Keep official links available.
- Distinguish introduced text from final text.
- Distinguish proposal/resolution/law.
- Make summaries optional and visibly generated.
- Reveal authorship after vote.
- Allow users to skip.
- Treat skip as an engagement signal, not a political abstention.
- Store support, oppose, and skip as separate interaction types.
- Do not compare user vote to final law when they saw initial text.
- Show speciality/final-global votes in lifecycle/details rather than the main swipe loop.

Nice-to-have guardrails:

- "What changed later?" section for initiatives with final text.
- "Discussed together with..." section when debate/final vote references multiple initiatives.
- Confidence/warning badge for partial extraction or summary issues.
- User correction/report flow for bad summaries or redactions.
- Basic feed diversification so high-skip or overexposed proposals do not dominate.

## Implementation Priorities

Recommended product implementation order:

1. Import structured initiative metadata, authors, events, votes, official links.
2. Build card UI using title/type/official text link and generality vote result.
3. Add redacted introduced text once extraction/redaction works.
4. Add post-vote reveal with party vote breakdown.
5. Add debate/video/source links in detail view.
6. Add skip tracking and a simple feed-interest score using raw counters.
7. Add optional AI summaries via queued background jobs.
8. Add richer user analytics/agreement views.

Do not block the first usable version on AI summarization. The app can launch with official metadata, redacted text where available, and source links, then progressively fill summaries as budget allows.

## Short Handoff Summary

- The main card should ask whether the user supports the introduced initiative in principle.
- Compare the user primarily with the `250` generality vote.
- Hide party/deputy identity before the vote, reveal it after.
- Treat skip as separate from abstention and use it for feed quality.
- Use both support and oppose as positive engagement signals for ranking; penalize repeated skips carefully.
- Keep speciality/final-global votes in the lifecycle/details experience, not the main swipe flow.
- Preserve official source, debate transcript, and video links.
- Label `Projeto de Resolução` as a resolution/recommendation, not a law.
- Treat AI summaries as optional, generated aids with clear disclaimers and audit metadata.
- Make it easy to go deeper without making the swipe card heavy.
