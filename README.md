# GamP_SCPeriop

## Project Concept
*GamP_SCPeriop* is a learning pathway management and gamification platform focused on perioperative education. The project combines the rigorous structuring of study plans (Pathways) with an interactive reward system. The goal is twofold: to make it easier for supervisors to track their students, and to break the monotony of traditional learning, boosting student motivation and focus through a gamified environment.

## Execution Loop
* **Structuring:** Supervisors create detailed pathways, divided into Modules, theoretical/practical Components, and Stage Timelines.
* **Cloning System (Templates):** Use of base templates to clone pre-defined structures and speed up the creation of new pathways.
* **Tracking (Dashboard):** Direct assignment of study plans to students and visual monitoring of each user's overall progress.
* **Core Gamification:** Students have access to an interactive "Achievements" (Badges) interface, where they can view the medals they've earned as they progress through their pathway.

## Running locally
* Requires the .NET 10 SDK and SQL Server LocalDB (installed with Visual Studio). Settings are in `GamP-SCPeriop.Server/appsettings.Development.json`.
* Start: `dotnet run --project GamP-SCPeriop.Server --launch-profile https`, then open https://localhost:7038.
* Test data (deletes everything in the database first): `.\scripts\seed.ps1` on Windows or `./scripts/seed.sh` on Linux/macOS/Git Bash. Add `-Force` / `-f` to skip the confirmation, or `-ConnectionString` / `-c` to seed another database.
* All test accounts use `first.last@gamp.test` with the password set in `DbSeeder.TestPassword` (`GamP-SCPeriop.Server/Data/DbSeeder.cs`). The test panel at https://dashboard.dev.localhost:7038 switches between them without logging in.

## Future Ideas
* **Trigger Engine & Notifications:** Create a practical workflow to add new Badges and define the exact actions that trigger them (e.g., module completion %). Implement real-time alerts ("New Achievement Unlocked!").
* **Visual Polish:** Improve the UI/UX of the badges (increase card contrast against the background) and introduce Tiers (e.g., 5 distinct levels) to add depth to the reward system.
* **Administration Panel:** Build an exclusive Admin Dashboard to centralize the management of Badges and Pathway Templates, removing complex parameters from the Supervisor's view.
* **Evaluation Evolution:** Integrate logical weights and define mandatory evaluation elements directly within the pathway templates.

##
ssh soliveira@IP