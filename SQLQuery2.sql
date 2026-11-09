RESTORE FILELISTONLY
FROM DISK = '/var/opt/mssql/backup/RaceDay.bak';

USE master;
GO

RESTORE DATABASE RaceDayDb
FROM DISK = '/var/opt/mssql/backup/RaceDay.bak'
WITH
    MOVE 'RaceDayDb'
        TO '/var/opt/mssql/data/RaceDayDb.mdf',
    MOVE 'RaceDayDb_log'
        TO '/var/opt/mssql/data/RaceDayDb_log.ldf',
    RECOVERY,
    STATS = 10;
GO