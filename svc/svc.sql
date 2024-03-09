-- grants access from Dodkin db to the virtual service account

use master;
go
create login [NT SERVICE\Dodkin] from windows with default_database=[Dodkin];
go

use Dodkin;
go
create user [NT SERVICE\Dodkin] for login [NT SERVICE\Dodkin];
go
alter user [NT SERVICE\Dodkin] with default_schema=[job];
go

go
alter role [db_datareader] add member [NT SERVICE\Dodkin];
go
alter role [db_datawriter] add member [NT SERVICE\Dodkin];
go
grant insert on schema::[job] to [NT SERVICE\Dodkin];
go
grant select on schema::[job] to [NT SERVICE\Dodkin];
go
grant update on schema::[job] to [NT SERVICE\Dodkin];
go
grant delete on schema::[job] to [NT SERVICE\Dodkin];
go
grant execute on schema::[job] to [NT SERVICE\Dodkin];
go
