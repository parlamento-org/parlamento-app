# Parlamento App

The Parlamento App helps people browse Portuguese parliamentary initiatives through neutral, redacted proposal content and derived legislative metadata.

## Language

**Project Law**:
A parliamentary initiative tracked as the core proposal record in the app, regardless of whether the source initiative is formally a projeto de lei, proposta de lei, or another initiative type.
_Avoid_: Proposal row, initiative record

**Redacted Proposal Text**:
The proposal text after proposer, party, parliamentary group, and other identifying terms have been removed for neutral downstream processing.
_Avoid_: Raw proposal text, full proposal text

**Proposal Topic Taxonomy**:
A reviewed versioned set of subject categories used to describe what a Project Law is about.
_Avoid_: Cluster labels, tags

**Parent Topic**:
A broad top-level subject in a Proposal Topic Taxonomy that groups related Subtopics.
_Avoid_: Category, theme

**Subtopic**:
A specific reviewed subject within a Parent Topic that can be assigned to a Project Law.
_Avoid_: Cluster, label

**Topic Assignment**:
The current or historical association between a Project Law and a Subtopic for a specific Proposal Topic Taxonomy version.
_Avoid_: Classification row, topic tag
