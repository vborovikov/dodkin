use Dodkin;
go

--v0.6.1

begin transaction;
go

alter table job.Delivery add
	MessageLabel nvarchar(249) null;
go

commit transaction;
go