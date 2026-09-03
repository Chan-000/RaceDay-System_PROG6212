# RaceDay Event Management System

## Project Description
RaceDay is a web-based Event Management System that is designed to support road running, walking, and cycling events in South Africa. The system allows Organisers to manage events, categories, participant enrolments and results. Participants can register for events, select an event category, view their enrolments and track their results.
This repository contains the planning and database work completed for PART 1 of the RaceDay project.

## User Roles

### Organiser
Organisers are responsible for managing events and participant information. They can:-
- Create, edit and delete events
- Manage event categories
- View all enrolments for their events
- Capture participant results
  
### Participant
Participants use the system to: -
- Create an account and log in
- Browse available events
- Enrol in an event by selecting a category
- View their own enrolments
- Track personal race results

## Part 1
Part 1 focuses on the planning and database design of the RaceDay system.

The following deliverables are included: -

1. Entity Relationship Diagram (ERD)
2. API Endpoint Plan
3. SQL Server Database Script
4. GitHub repository and GitHub Actions CI validation

## Repository Structure
RaceDay/
|
|-- README.md
|
|-- docs
| |-- RaceDay_ERD.pdf
| |-- RaceDay_API_Endpoint_Plan.pdf
| `-- RaceDay_Database.sql
|
`-- .github
 `-- workflows
   `--part1-ci-yml

## Database Setup
1. Open SQL Server Management Studio (SSMS)
2. Connect to your local SQL Server instance
3. Open the file 'docs/RaceDay_Database.sql'
4. Execute the entire script or press 5
5. View the inserted sample data using SELECT queries

## GitHub Actions (CI/CD)
A GitHub Action validates the required Part 1 documentation and repository structure.

The Part 1 workflow checks that the following files and folders exist:-
- docs/
- docs/RaceDay_ERD.pdf
- docs/RaceDay_API_Endpoint_Plan.pdf
- docs/RaceDay_Database.sql
- README.md

- CI Build Status
<img width="655" height="175" alt="image" src="https://github.com/user-attachments/assets/4db28158-f599-40c0-bbef-04b55281641a" />

## YouTube Link
