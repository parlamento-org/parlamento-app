# Development

## Development guide

<!--
Explain what a new developer to the project should know in order to develop the system, including who to build, run and test it in a development environment.

Document any APIs, formats and protocols needed for development (but don't forget that public APIs should also be accessible from the "How to use" section in your README.md file).

Describe coding conventions and other guidelines adopted by the project.
-->

#### Branching strategy and Pull-Request Policy

The version-control system will also follow a convention concerning the branching strategy. The used branching strategy will be `GitFlow`.

`GitFlow` has the following types of branches:

| Branch Name | Branch Type      | Description                                                                          | Naming Convention         |
| ----------- | ---------------- | ------------------------------------------------------------------------------------ | ------------------------- |
| `main`      | Single Branch    | Contains the latest stable version of the project.                                   | `main`                    |
| `develop`   | Single Branch    | Contains the current global working version of the project.                          | `develop`                 |
| `feature`   | Auxiliary Branch | Contains a working version of a new feature.                                         | `feature/name-of-feature` |
| `fix`       | Auxiliary Branch | Contains a working version on a bugfix.                                              | `fix/name-of-fix`         |
| `hotfix`    | Auxiliary Branch | Contains a working version on a hotfix for a stable version.                         | `hotfix/name-of-fix`      |
| `release`   | Auxiliary Branch | Contains a working version which is helping to prepare for a new production release. | `release/name-of-release` |

Single Branches (`main` and `develop`) have an infinite lifetime and are unique. There can be as many Auxiliary Branches as needed, and their lifetime is limited to the end of the task they are responsible for. For example, `feature/dashboard-dropdown` is alive until the dashboard's dropdown is finished and the branch is merged with develop.

`main` contains the latest stable version of the project and thus the default branch. `develop` is the main development branch, and thus the most up-to-date working version of the project. `feature` and `fix` branches branch out from and are merged back to `develop`. `release` branches also branch out from `develop` and are merged back to `develop` but also to `main`. `hotfix` branches branch out from `main` and are merged back to both `develop` and `main`.

For reference, each User Story should branch out of `develop` in a `feature` branch. Work items should branch out from and be merged back to the `feature` branch of the associated User Story.

#### Releases

On each release to the `main` branch, a `GitHub` release will also be made describing notable changes, the contributors and significant assets (if applicable). This release's name will follow the Sprint number: for Sprint 1, the release will be named `1.0`. Any following hotfixes will add up on the second number - that is, the first hotfix on the first release will be named `1.1`.

_Pull requests_ should also be reviewed by at least two team members before merging. This reviews should be done with the utmost attention to ensure that the code is correct, clean, clear and maintainable.

#### Commit Message Standard

Commit messages should also follow a well-defined structure. As for basic rules, the commit messages:

- Should not end the subject line with a period
- Should capitalize the subject line and each paragraph
- Should use the imperative mood in the subject line
- Should wrap lines at 72 characters
- Should use the body (if needed) to explain what and why you have done something. In most cases, you can leave out details about how a change has been made.
- Should separate subject from body with a blank line (if the body is present)

For other commit guidelines check [this page](https://gist.github.com/robertpainsi/b632364184e70900af4ab688decf6f53).

#### Coding Conventions

For the frontend, we're using Google's standard _code-style_ for `Flutter`.

`GitHub Actions` were set up to run these linters. As such, for a _pull request_ to be accepted, the actions must pass, meaning that the code must properly follow the defined conventions.

## Security concerns

Identify potential security vulnerabilities classes and explain what the team has done to mitigate them.

## Metrics

Describe how you captured each of the "four key metrics" (e.g., automatic mechanism, survey to the team on the end of each sprint).

Provide here an historical record (for each sprint) of the four key metrics and velocity.
