insert into Organiser(Name,Email,Password,OrganisationName)
values('Sarah Dlamini', 'sarah@runwildevents.co.za', 'hashed_pw_1', 'RunWild Events'),
('Peter Johns', 'peter@stridesportsza.co.za', 'hashed_pw_2', 'Stride Sports SA');

insert into Participant(Name,Email,Password,DateOfBirth,EmergencyContact)
values('Thandiwe Dube', 'thandidube@gmail.com', 'hashed_pw_3', '1995-05-24', '082 555 0102')
('James Botha', 'james@gmail.com', 'hashed_pw_4', '1988-12-03', '063 023 4589');

insert into Event(OrganiserID, EventName,EventDate,Location,Description)
values(1,'Centurion Sunrise Run','2026-10-12','Centurion, Gauteng','A community road ruuning event through Centurion.'),
(2,'Heritage Charity Walk','2026-09-25','Pretoria,Gauteng','Charity walk raising funds for local schools.'),
(3,'Cape Coastal Cycle Challenge','2026-11-12','Cape Twon, Western CApe','A scenic cyclying event along the Cape coastline.');
