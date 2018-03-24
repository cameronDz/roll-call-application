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
Add walk through steps on Nuget package manager, Setting Properties configuration, feature flags,
and Azure deployment for code first.

## Updates List

#### Accepted Development/Task/Wish List for next Pull Request into master branch
Below are a list of wish list items for updates to the application. 

- [ ] Create standard feature switch to be used for all new features
- [ ] Create standard branching schema
- [ ] Ability to lock EventGuest Index View with basic encrypted password
- [ ] Graphical display of EventGuest check in traffic for a given day
- [ ] Paginated list of checked in guests with grid size set in System variables
- [ ] Create set up steps for pulling, customizing, and deploying application

###### 23 March 2018
Basic project commited to git. Features include:
- Basic Home, About, and Contact page explaining application
- Event Guest model with controller and view for; Index, Create, Edit, and Delete
- Ability to pull list of all guests checked in onto personal machine as .csv file