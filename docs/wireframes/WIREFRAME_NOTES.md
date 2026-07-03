# Wireframe Notes

These wireframes describe the intended product flow.

## main_proposal_card.png

Main pre-vote experience.
Important requirements:

- Hide proposer/party/deputy identity.
- Show initiative type.
- Show neutral title.
- Show AI summary if available.
- Show redacted excerpt.
- Support / Oppose / Skip actions.

## back_of_card_redacted_text_needs_space_for_ai_summary.png

Back of the swipe card, shown after the user has tapped on the card to flip it over, revealing the redacted formatted text.
Initially there was no AI summary, so there isnt any space dedicated for it here. Try to figure out somewhere in the pre-vote part of the flow where the AI summary could fit in. Feel free to suggest a different UI/UX flow.

Important requirements:

- Display the redacted proposal text in a comfortable reading layout.
- Reserve space for an optional AI-generated summary.

## reveal_outcome_approval.png

Shown immediately after the user submits their vote.

Purpose:
Reveal how Parliament actually voted and allow the user to compare their own position with the parliamentary outcome before deciding whether to explore further.

Important requirements:

- Clearly communicate whether the initiative was approved or rejected at the Generality vote (phase 250).
- Display the initiative's proposer (party, government, deputies, committee, etc.).
- Show which parliamentary parties voted in favour, against, abstained, and were absent.
- Visually highlight whether the user's vote aligned with the parliamentary outcome.
- Present the information in a simple, celebratory/informative way rather than overwhelming the user with legislative details.
- Include a prominent "Proposal Journey Timeline" action leading to the complete Proposal Journey Timeline (described in APP_PRODUCT_FLOW_GUIDE.md)
- The full detail page should contain the complete lifecycle, official documents, debate transcripts, videos, AI summary, and other contextual information.
- This screen should remain lightweight and serve as the transition between the swipe experience and the deeper exploration experience.

## reveal_outcome_rejected.png

Shown immediately after the user submits their vote.

Purpose:
Reveal that the initiative was rejected during the Generality vote and allow the user to compare their own position with Parliament before exploring additional context.

Important requirements:

- Clearly communicate that the initiative was rejected at the Generality vote (phase 250).
- Display the initiative's proposer (party, government, deputies, committee, etc.).
- Show which parliamentary parties voted in favour, against, abstained, and were absent.
- Visually highlight whether the user's vote matched or differed from Parliament's decision.
- Keep the presentation concise and easy to understand without exposing excessive legislative detail.
- Include a prominent "Proposal Journey Timeline" action leading to the complete Proposal Journey Timeline (described in APP_PRODUCT_FLOW_GUIDE.md)
- The detail page should provide the full legislative history, official documents, debates, videos, AI summary, and all supporting information.
- This screen should conclude the voting interaction while encouraging users to learn more if they wish.

## previous_votes_history.png

History screen showing the user's previous interactions with initiatives.

Purpose:
Allow users to revisit proposals they have already voted on or skipped, search through their history, and quickly see how they reacted.

Important requirements:

- Show a list of initiatives the user has already interacted with.
- Each list item should display:
  - proposer identity/logo, since this is post-vote history
  - initiative title
  - the user's recorded action: support, oppose, or skip
- Use clear visual indicators:
  - green check for support
  - red X for oppose
  - neutral/grey indicator for skip if implemented
- Include a search field for filtering previous votes by title, proposer, type, or topic.
- Include a filter/sort action for narrowing by vote type, initiative type, proposer, date, or parliamentary outcome.
- Tapping an item should open the corresponding "post-vote screen" and then from there users can get to the proposal journey/detail page.
- This screen should use post-reveal data only; there is no need to anonymize proposer identity here.
- Keep user skips separate from political abstentions.
