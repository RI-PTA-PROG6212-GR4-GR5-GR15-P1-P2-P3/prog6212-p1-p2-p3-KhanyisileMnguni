Create Table Organiser (
OrganiserID int identity(1,1) primary key,
Name nvarchar(100) not null,
Email nvarchar(150) not null unique,
Password nvarchar(200) not null,
OrganisationName nvarchar(150) null
);

Create Table Participant(
ParticipantID int identity(1,1) primary key,
Name nvarchar(100) not null,
Email nvarchar(150) not null unique,
Password nvarchar(200) not null,
DateOfBirth date not null,
EmergencyContact nvarchar(100) null
);

Create Table Event(
EventID int identity(1,1) primary key,
OrganiserID int not null,
EventName nvarchar(100) not null,
EventDate date not null,
Location nvarchar(150) not null,
Description nvarchar(1000) null,
CONSTRAINT fk_Event_Organiser FOREIGN KEY (OrganiserID) REFERENCES Organiser(OrganiserID)
);

Create Table Category(
CategoryID int identity(1,1) primary key,
EventID int not null,
CategoryName nvarchar(50) not null,
DistanceKM decimal(5,2) not null,
MaxParticipants int not null default 100,
EntryFee decimal(8,2) not null default 0,
CONSTRAINT fk_Catergory_Event foreign key (EventID) references Event(EventID)
);

--Enrolment resolves the many-to-many relationship between Participant and Caategory (a Participant can enter many Categories across many Events; a Ctegory can have many Participants).
Create Table Enrolment(
EnrolmentID int identity(1,1) primary key,
ParticipantID int not null,
CategoryID int not null,
EnrolmentDate date not null default cast(getdate() as date),
Status nvarchar(30) not null default 'Registered',
CONSTRAINT fk_Enrolment_Participant foreign key (ParticipantID) references Participant(ParticipantID),
CONSTRAINT fk_Enrolment_Category foreign key(CategoryID) references Category(CategoryID),
CONSTRAINT uq_Enrolment UNIQUE (ParticipantID, CategoryID)
);

Create Table Result(
ResultID int identity(1,1) primary key,
EnrolmentID int not null unique,
FinishTime time null,
Position int null,
Status nvarchar(30) not null default 'Finished',
CONSTRAINT fk_Result_Enrolment foreign key(EnrolmentID) references Enrolment(EnrolmentID)
);