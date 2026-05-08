# Parlamento App

A mobile-first civic-tech application inspired by swipe-based social interfaces, designed to make Portuguese parliamentary proposals more accessible and engaging to everyday users.

The application allows users to interact with real law proposals admitted to the Portuguese Parliament by swiping:

- right to approve
- left to reject
- abstain when undecided

Based on voting patterns, the app generates political alignment statistics, showing which political parties the user most frequently agrees or disagrees with.

The project also includes tooling for scraping and processing parliamentary proposal data from the Portuguese Parliament APIs.

---

# Features

- Swipe-based proposal voting interface
- Real parliamentary proposal integration
- Voting statistics and political alignment insights
- Party agreement/disagreement analysis
- User authentication
- Mobile-first UX
- Proposal ingestion and database population workflows

---

# Tech Stack

## Frontend

- Flutter
- Dart

## Backend

- ASP.NET Core Web API
- C#
- Entity Framework Core
- SQLite

## Tooling & Infrastructure

- Swagger/OpenAPI
- FluentValidation
- Prometheus monitoring packages
- Health check endpoints

---

# Concept

The project explores how modern mobile interaction patterns can be applied to political participation and civic engagement.

Instead of presenting parliamentary information through traditional government interfaces, the application experiments with:

- lightweight interaction patterns
- gamified political discovery
- behavioral statistics
- accessible proposal exploration

The overall goal was to reduce friction between users and political information.

---

# Statistics System

Based on user voting history, the application computes:

- political party alignment trends
- agreement/disagreement percentages
- voting behavior summaries

Future planned improvements included:

- category-based breakdowns (environment, transportation, welfare, etc.)
- timeline-based voting evolution
- proposal recommendation systems

---

# Data Pipeline

The repository also documents the process used to:

- retrieve parliamentary proposal data from public APIs
- normalize proposal information
- populate the application database
- expose proposal endpoints to the frontend application

---

# Architecture

## Frontend

The frontend mobile application was developed with Flutter and designed around swipe-based interactions inspired by dating/social applications.

The interface communicates with the backend API to:

- retrieve proposals
- authenticate users
- submit voting interactions
- display statistics

---

## Backend

The backend is an ASP.NET Core Web API responsible for:

- proposal persistence
- API endpoints
- validation
- user-related operations
- proposal statistics handling

The backend also includes:

- Swagger/OpenAPI documentation
- FluentValidation integration
- health check endpoints
- monitoring instrumentation

---

# Motivation

This project was developed as an experiment in civic-tech and interaction design, combining:

- mobile application development
- public API integration
- political data visualization
- gamified UX concepts
- backend API development

It also provided practical experience with:

- Flutter application architecture
- REST API development
- proposal data ingestion pipelines
- user interaction analytics

---

# Repository Structure

```text
/frontend   → Flutter mobile application
/backend    → ASP.NET Core API and persistence layer
```

---

# Future Improvements

- Proposal recommendation algorithms
- More advanced political analytics
- Proposal categorization systems
- Real-time parliamentary updates
- Social/community voting features
- Expanded data visualization dashboards

---
