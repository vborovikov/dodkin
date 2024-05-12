use Dodkin;
go

--v0.6.1

begin transaction;
go

alter table job.Delivery drop column MessageLabel;
go

commit transaction;
go