/*
RaceDay Event Management System
Part 1 SQL database script which matches the final
ERD and API endpoint plan
*/

--Drops the database if it already exists
IF DB_ID('RaceDay') IS NOT NULL
BEGIN 
   ALTER DATABASE RaceDay SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
   DROP DATABASE RaceDay;
END
GO

-- create database
CREATE DATABASE RaceDay;
GO

USE RaceDay;

-- users table
CREATE TABLE Users
(
   UserID       INT IDENTITY(1,1) PRIMARY KEY,
   FirstName    VARCHAR(50) NOT NULL,
   LastName     VARCHAR(50) NOT NULL,
   Email        VARCHAR(100) NOT NULL UNIQUE,
   PasswordHash VARCHAR(255) NOT NULL,
   Role         VARCHAR(20) NOT NULL CHECK (Role IN ('Organiser', 'Participant')),
   PhoneNumber  VARCHAR(20) NULL
);
GO

-- EventType table
CREATE TABLE EventType 
(
   EventTypeID INT IDENTITY(1,1) PRIMARY KEY,
   TypeName VARCHAR(50) NOT NULL UNIQUE,
   Description VARCHAR(255) NULL
);

-- Events table
CREATE TABLE Events
(
   EventID         INT IDENTITY(1,1) PRIMARY KEY,
   EventName       VARCHAR(150) NOT NULL,
   Description     VARCHAR(500) NULL,
   EventDate       DATE NOT NULL,
   Location        VARCHAR(180) NOT NULL,
   DistanceKm      DECIMAL(6,2) NOT NULL,
   MaxParticipants INT NOT NULL,
   EventTypeID     INT NOT NULL,
   OrganiserID     INT NOT NULL,

   CONSTRAINT FK_Events_EventType FOREIGN KEY (EventTypeID) REFERENCES EventType(EventTypeID),
   CONSTRAINT FK_Events_Organiser FOREIGN KEY (OrganiserID) REFERENCES Users(UserID),
   CONSTRAINT CK_Events_Distance CHECK (DistanceKm > 0),
   CONSTRAINT CK_Events_MaxParticipants CHECK (MaxParticipants > 0)
);
GO

-- Category table
CREATE TABLE Category 
( 
   CategoryID   INT IDENTITY(1,1) PRIMARY KEY,
   EventID      INT NOT NULL,
   CategoryName VARCHAR(100) NOT NULL,
   AgeMin       INT NULL,
   AgeMax       INT NULL,
   EntryFee     DECIMAL(10,2) NOT NULL DEFAULT 0,

   CONSTRAINT FK_Category_Event FOREIGN KEY (EventID) REFERENCES Events(EventID),
   CONSTRAINT CK_Category_AgeRange CHECK (AgeMin IS NULL OR AgeMax IS NULL OR AgeMin <= AgeMax),
   CONSTRAINT CK_Category_EntryFee CHECK (EntryFee >= 0)
);
GO

-- Enrolment table
CREATE TABLE Enrolment
(
   EnrolmentID     INT IDENTITY(1,1) PRIMARY KEY,
   ParticipantID   INT NOT NULL,
   EventID         INT NOT NULL,
   CategoryID      INT NOT NULL,
   EnrolmentDate   DATE NOT NULL DEFAULT GETDATE(),
   EnrolmentStatus VARCHAR(20) NOT NULL DEFAULT 'Confirmed' CHECK (EnrolmentStatus IN ('Confirmed', 'Cancelled')),

   CONSTRAINT FK_Enrolment_Participant FOREIGN KEY (ParticipantID) REFERENCES Users(UserID),
   CONSTRAINT FK_Enrolment_Event FOREIGN KEY (EventID) REFERENCES Events(EventID),
   CONSTRAINT FK_Enrolment_Category FOREIGN KEY (CategoryID) REFERENCES Category(CategoryID),
   CONSTRAINT UQ_Enrolment_Participant_Event UNIQUE (ParticipantID, EventID)  
);
GO 

-- Results table
CREATE TABLE Results 
(
   ResultID INT IDENTITY(1,1) PRIMARY KEY,
   EnrolmentID INT NOT NULL UNIQUE,
   FinishTime TIME NULL,
   FinishPosition INT NULL,

   CONSTRAINT FK_Result_Enrolment FOREIGN KEY (EnrolmentID) REFERENCES Enrolment(EnrolmentID)
);

-- inserting event types
INSERT INTO EventType (TypeName, Description) VALUES
('Run', 'Running events including marathons and fun runs'),
('Walk', 'Walking events'),
('Cycle', 'Road cycling and cycle tours');
GO

-- insert users
INSERT INTO Users (FirstName, LastName, Email, PasswordHash, Role, PhoneNumber) VALUES
('Mondli', 'Nkosi', 'mondli@raceday.co.za', 'hashed_password_1', 'Organiser', '0745093344'),
('Taehyung', 'Kim', 'taehyung@raceday.co.za', 'hashed_password_2', 'Organiser', '0657321678'),
('Bonang', 'Matheba', 'bonang@email.com', 'hashed_password_3', 'Participant', '0813114036'),
('Naledi', 'Smith', 'naledi@email.com', 'hashed_password_4', 'Participant', '0623789050');
GO

-- insert events
INSERT INTO Events (EventName, Description, EventDate, Location, DistanceKm, MaxParticipants, EventTypeID, OrganiserID) VALUES
('Two Oceans Marathon', 'A famous ultra marathon and half-marathon held annually in Cape Town ', '2026-09-13', 'Cape Town, Western Cape', 10.00, 500, 1, 1),
('Limpopo Family Walk', 'Community walking event for participants of all ages', '2026-10-24', 'Polokwane, Limpopo', 5.00, 250, 2, 2),
('Pretoria Cycle Challenge', 'Road cycling challenge around Pretoria', '2026-11-07', 'Pretoria, Gauteng', 80.00, 200, 3, 1);
GO

-- insert categories
INSERT INTO Category (EventID, CategoryName, AgeMin, AgeMax, EntryFee) VALUES
(1, 'Junior', 13,39, 200.00),
(1, '40-49', 40,49, 250.00),
(1, 'Veteran', 50,99, 100.00),
(2,'Junior', 13,19, 50.00),
(2,'Adult Walk', 20,59, 50.00),
(2,'Veteran Walk', 60,99, 40.00),
(3,'Open Cycle', 18,39, 150.00),
(3,'Veteran Cycle', 40,99, 120.00);
GO

-- insert enrolments
INSERT INTO Enrolment (ParticipantID, EventID, CategoryID, EnrolmentStatus) VALUES
(3, 1, 2, 'Confirmed'),
(4, 1, 2, 'Confirmed'),
(3, 2, 5, 'Cancelled'),
(4, 3, 7, 'Confirmed');
GO

-- insert results 
INSERT INTO Results (EnrolmentID, FinishTime, FinishPosition) VALUES
(1, '00:52:34', 12),
(2, '00:58:21', 18);

