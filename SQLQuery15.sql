insert into Organiser(Name,Email,Password,OrganisationName) values
('Sarah Dlamini', 'sarah@runwildevents.co.za', 'hashed_pw_1', 'RunWild Events'),
('Peter Johns', 'peter@stridesportsza.co.za', 'hashed_pw_2', 'Stride Sports SA');

insert into Participant(Name,Email,Password,DateOfBirth,EmergencyContact) values
('Thandiwe Dube', 'thandidube@gmail.com', 'hashed_pw_3', '1995-05-24', '082 555 0102')
('James Botha', 'james@gmail.com', 'hashed_pw_4', '1988-12-03', '063 023 4589');

insert into Event(OrganiserID, EventName,EventDate,Location,Description) values
(1,'Centurion Sunrise Run','2026-10-12','Centurion, Gauteng','A community road ruuning event through Centurion.'),
(2,'Heritage Charity Walk','2026-09-25','Pretoria,Gauteng','Charity walk raising funds for local schools.'),
(3,'Cape Coastal Cycle Challenge','2026-11-12','Cape Twon, Western CApe','A scenic cyclying event along the Cape coastline.');

insert into Category(EventID,CategoryName,DistanceKM,MaxParticipants,EntryFee) values
(1.'5km Fun Run',5.00,300,100.00),
(2,'10km Road Race', 10.00,200,150.00),
(2,'5km Walk',5.00,500,50,00),
(3,'40km Road Cycle', 40.00,250,250.00),
(3,'80km Road Cycle',80.00,150,350,00);

insert into Enrolment(ParticipantID,CategoryID,EnrolmentDate,Status) values
(1,2,'2026-08-01','Registered'),
(1,4,'2026-08-05','Registered'),
(2,1,'2026-08-02','Registered'),
(2,5,'2026-08-06','Registered');
