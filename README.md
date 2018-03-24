# Roll Call Application

## Application Overview
The purpose of this application is a simple check in application to keep track of guests entering
and event.

## Application Development
Application is a basic ASP.NET MVC 5 application that uses Entity Framework Code First feature 
to generate the back end. The customizability of this application should be maintained throughout
development, so that all new features added have features switches in the settings, as well as 
String variables so that only some default property settings can be updated and the project can
be deployed on Azure. Basic deployment of the application should cost less than $5 for any event
that has less than 500 guests checking in on a single day.

## Setting up and deploying customized application
Add walk through steps on Nuget packages, Setting Properties configuration, feature flags,
and Azure deployment for code first.

#### Project default settings
After pulling the project down from GitHub, cleaning and building the project should restore all 
required Nuget packages. The Settings are currently set to the following, and should be updated
to customize the application out of the box.

| Setting Name | Type | Default Value | Description |
| :--- | :---: | :--- | :--- |
| EventName | String | "Event Name" | Name of event application will be used for. Name appears in various spots throughout the application. |
| CreatorEmail | String | "CameronDziurgot@gmail.com" | Email of the application creator. |
| GitHubRepositoryAddress | String | "https://github.com/cameronDz/roll-call-application" | Repository where the application can be found. |
| LockIndexFlagTurnedOn | bool | True | Feature flag for creating authentication that locks the Views in EventGuests for Index, Edit, and Delete, behind a passcode. |
| IndexPasscode | String | "rollCallApplication" | Passcode used when the Passcode feature flag is turned on. |

#### Deploying Application in Microsoft's Azure cloud

## Updates List

#### Accepted Development/Task/Wish List for next Pull Request into master branch
Below are a list of wish list items for updates to the application. 

- [x] Create standard feature switch to be used for all new features
- [x] Ability to lock EventGuest Index View with basic encrypted password
- [x] Create set up steps for pulling and customizing application through Settings 
- [ ] Graphical display of EventGuest check in traffic for a given day
- [ ] Paginated list of checked in guests with grid size set in System variables
- [ ] Create set up steps for deploying application

###### 24 March 2018
- Ability to lock EventGuest Index View with basic encrypted password
- Create standard feature switch to be used for all new features
- Create set up steps for pulling and customizing application through Settings 

###### 23 March 2018
Basic project commited to git. Features include:
- Basic Home, About, and Contact page explaining application
- Event Guest model with controller and view for; Index, Create, Edit, and Delete
- Ability to pull list of all guests checked in onto personal machine as .csv file